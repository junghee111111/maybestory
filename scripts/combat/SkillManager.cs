using Godot;
using System;
using System.Collections.Generic;

public partial class SkillManager : Node
{
    public static SkillManager Instance { get; private set; }

    private const float CriticalMultiplier = 1.5f;
    private const float HeadshotMultiplier = 1.5f;

    [Export] public Godot.Collections.Array<SkillData> AllSkills = new(); // 에디터에서 .tres 등록
    [Export] public bool DebugDrawVirtualHitbox = false;

    private readonly Dictionary<string, SkillData> _skillDataDict = new();
    private readonly Dictionary<string, int> _allocatedPoints = new();
    private readonly Dictionary<string, float> _activeBuffTimers = new(); // SkillId -> 남은 시간

    private Player _player;
    private PlayerStats _stats;
    private AnimationPlayer _animPlayer;
    private EquipManager _equipManager;

    public override void _Ready()
    {
        Instance = this;

        _player = GetOwner<Player>();
        _stats = _player.GetNode<PlayerStats>("PlayerStats");
        _animPlayer = _player.GetNode<AnimationPlayer>("BaseChar/AnimationPlayer");
        _equipManager = _player.GetNode<EquipManager>("EquipManager");

        foreach (var data in AllSkills)
        {
            _skillDataDict[data.SkillId] = data;
            _allocatedPoints[data.SkillId] = 0; // 초기 스킬 레벨 0
        }

        // 기본 기능은 allocatedPoints 1로 설정
        _allocatedPoints["SkillAttack"] = 1;
        _allocatedPoints["SkillJump"] = 1;
        _allocatedPoints["SkillPickUp"] = 1;
    }

    public SkillData GetSkillData(string skillId)
    {
        return _skillDataDict.TryGetValue(skillId, out var data) ? data : null;
    }

    // 저장/복원용: 현재 할당된 스킬 포인트를 모두 읽어온다.
    public IReadOnlyDictionary<string, int> GetAllAllocatedPoints() => _allocatedPoints;

    // 저장 파일을 불러옴 시 포인트 소모 검증 없이 값을 그대로 덩어씁은다.
    public void RestoreAllocatedPoints(IReadOnlyDictionary<string, int> allocatedPoints)
    {
        if (allocatedPoints == null) return;
        foreach (var (skillId, level) in allocatedPoints)
        {
            _allocatedPoints[skillId] = level;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // 버프 지속시간 차감
        var keys = new List<string>(_activeBuffTimers.Keys);
        foreach (var id in keys)
        {
            _activeBuffTimers[id] -= dt;
            if (_activeBuffTimers[id] <= 0)
            {
                _activeBuffTimers.Remove(id);
                OnBuffExpired(id);
            }
        }
    }

    // ==========================================
    // 1. 패시브/포인트 시스템
    // ==========================================
    public int GetSkillLevel(string skillId)
    {
        return _allocatedPoints.TryGetValue(skillId, out int lv) ? lv : 0;
    }

    public bool InvestPoint(string skillId)
    {
        if (_stats.SkillPoints <= 0) return false;
        if (!_skillDataDict.TryGetValue(skillId, out var data)) return false;

        int currentLv = GetSkillLevel(skillId);
        if (currentLv >= data.MaxSkillLevel) return false;

        _allocatedPoints[skillId] = currentLv + 1;
        _stats.SkillPoints--;
        return true;
    }

    // ==========================================
    // 액티브 스킬 구사 (통합 엔트리 포인트)
    // ==========================================
    public bool CastSkill(string skillId)
    {
        int level = GetSkillLevel(skillId);
        if (level <= 0) return false; // 아직 안 찍은 스킬

        if (!_skillDataDict.TryGetValue(skillId, out var skill)) return false;

        // 줍기는 비용/쿨타임/애니메이션 없이 즉시 처리한다.
        if (skill.Type == SkillType.ActivePickUp)
        {
            _player.TryPickUp();
            return true;
        }

        // MP 체크 및 소모
        int mpCost = skill.GetMpCost(level);
        if (_stats.CurrentMp < mpCost)
        {
            GD.Print("[Skill] MP가 부족합니다.");
            return false;
        }

        // HP 체크 및 소모
        int hpCost = skill.GetHpCost(level);
        if (_stats.CurrentHp < hpCost)
        {
            GD.Print("[Skill] HP가 부족합니다.");
            return false;
        }

        _stats.ConsumeMp(mpCost);
        _stats.ConsumeHp(hpCost);

        // 캐스팅 모션 랜덤하게 재생.
        string castAnim = skill.CastAnimationName.Length > 0 ? skill.CastAnimationName[GD.Randi() % skill.CastAnimationName.Length] : "";
        if (!string.IsNullOrEmpty(castAnim) && _animPlayer.HasAnimation(castAnim))
        {
            _animPlayer.Play(castAnim, 0.1f);
            _player.StartAttack();
        }

        // 스킬 타입별 분기 실행
        switch (skill.Type)
        {
            case SkillType.ActiveBuff:
                ExecuteBuff(skill, level);
                break;

            case SkillType.ActiveProjectile:
                ExecuteProjectile(skill, level);
                break;

            case SkillType.ActiveAutoTarget:
                ExecuteAutoTarget(skill, level);
                break;

            case SkillType.ActiveSummon:
                ExecuteSummon(skill, level);
                break;
        }

        return true;
    }

    // 2. 액티브 버프
    private void ExecuteBuff(SkillData skill, int level)
    {
        float duration = skill.GetDuration(level);
        _activeBuffTimers[skill.SkillId] = duration;
        GD.Print($"[Buff] {skill.SkillName} 발동! (지속시간: {duration}초)");
        // PlayerStats 능력치 가산 이벤트 발송
    }

    private void OnBuffExpired(string skillId)
    {
        GD.Print($"[Buff] {skillId} 지속시간 만료");
        // 능력치 원복
    }

    // 3. 투사체형 공격 (바라보는 방향으로 직진)
    private void ExecuteProjectile(SkillData skill, int level)
    {
        if (skill.ProjectileScene == null) return;

        var projInstance = skill.ProjectileScene.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(projInstance);

        // 플레이어 손/중심 위치 및 바라보는 방향 계산
        float facingDir = _player.GetNode<Node3D>("BaseChar").RotationDegrees.Y > 0 ? 1.0f : -1.0f;
        Vector3 spawnPos = _player.GlobalPosition + new Vector3(facingDir * 0.8f, 0.8f, 0.0f);
        projInstance.GlobalPosition = spawnPos;

        // 투사체 초기화 (방향, 속도, 대미지)
        (int calculatedDmg, bool isCritical) = CalculateDamage(skill, level);
        if (projInstance is Projectile proj)
        {
            proj.Initialize(new Vector3(facingDir, 0, 0), skill.ProjectileSpeed, calculatedDmg, isCritical);
        }
    }

    // 대미지/크리티컬 계산 (Luk 기반 크리 확률, 헤드샷 배율 적용)
    private (int Damage, bool IsCritical) CalculateDamage(SkillData skill, int level, bool isHeadshot = false)
    {
        float damage = (_stats.Int + _stats.Luk / 2) * skill.GetDamageMultiplier(level);
        if (skill.SkillId == "SkillAttack")
        {
            damage = _stats.Str * skill.GetDamageMultiplier(level);
        }


        float critChance = Mathf.Clamp(_stats.Luk * 0.005f, 0f, 0.5f);
        bool isCritical = GD.Randf() < critChance;

        if (isCritical) damage *= CriticalMultiplier;
        if (isHeadshot) damage *= HeadshotMultiplier;

        return (Mathf.RoundToInt(damage), isCritical);
    }

    private async void ExecuteAutoTarget(SkillData skill, int level)
    {
        if (skill.PreDelay > 0f)
        {
            await ToSignal(GetTree().CreateTimer(skill.PreDelay), SceneTreeTimer.SignalName.Timeout);
        }

        List<(Mob Mob, string Part)> targets = skill.UseAttackRangeBasedOnWeaponMesh
            ? FindTargetsInWeaponHitbox(skill)
            : FindTargetsInVirtualHitbox(skill);

        int attackCount = Mathf.Max(1, skill.AttackCount);
        foreach (var (target, part) in targets)
        {
            var damages = new int[attackCount];
            var criticals = new bool[attackCount];
            for (int i = 0; i < attackCount; i++)
            {
                (int dmg, bool isCritical) = CalculateDamage(skill, level, isHeadshot: part == "Head");
                damages[i] = dmg;
                criticals[i] = isCritical;
            }

            if (skill.HitVfxScene != null)
            {
                var vfx = skill.HitVfxScene.Instantiate<Node3D>();
                GetTree().CurrentScene.AddChild(vfx);
                // 몬스터 위치 + 상대 오프셋에 생성
                vfx.GlobalPosition = target.GlobalPosition + skill.HitVfxTargetOffset;
            }

            target.TakePartDamage(part, damages, _player.GlobalPosition, criticals, subText: part == "Head" ? "HeadShot!" : part == "Leg" ? "Leg Shot.." : "");
        }
    }

    // 몬스터당 가장 우선순위 높은 히트박스 부위(Head > Leg > 몸통)를 골라준다.
    private static string PickBestPart(string a, string b)
    {
        if (a == "Head" || b == "Head") return "Head";
        if (a == "Leg" || b == "Leg") return "Leg";
        return "Body";
    }

    // 무기(Weapon/Area3D/HitArea)와 겹쳐진 몬스터를 찾는다. 부위별 Area3D(Head/Leg/Body)가 있으면 맞은 부위도 함께 반환한다.
    private List<(Mob Mob, string Part)> FindTargetsInWeaponHitbox(SkillData skill)
    {
        var found = new Dictionary<Mob, string>();

        Area3D hitArea = _equipManager?.WeaponHitArea;
        if (hitArea == null) return new List<(Mob, string)>();

        foreach (Area3D area in hitArea.GetOverlappingAreas())
        {
            if (area.GetParent() is not Mob mob) continue;
            string areaName = area.Name.ToString();
            string part = areaName is "Head" or "Leg" ? areaName : "Body";
            found[mob] = found.TryGetValue(mob, out string existing) ? PickBestPart(existing, part) : part;
        }

        var targets = new List<(Mob, string)>();
        foreach (var (mob, part) in found)
        {
            if (targets.Count >= skill.TargetCount) break;
            targets.Add((mob, part));
        }
        return targets;
    }

    // AttackRangeW/H/D(장착 무기의 AttackRange 배율 적용) 크기의 가상 박스(플레이어 위치 + AttackRangeOffset)로 겹치는 대상을 찾는다.
    private List<(Mob Mob, string Part)> FindTargetsInVirtualHitbox(SkillData skill)
    {
        var targets = new List<(Mob Mob, string Part)>();

        float weaponRangeMultiplier = _equipManager?.GetEquipped(EquipSlot.Weapon)?.AttackRange ?? 1.0f;
        var size = new Vector3(skill.AttackRangeW * weaponRangeMultiplier, skill.AttackRangeH, skill.AttackRangeD);

        // 바라보는 방향으로 가로폭의 절반만큼 밀어 캐릭터 앞쪽만 판정되도록 한다.
        float facingDir = _player.GetNode<Node3D>("BaseChar").RotationDegrees.Y > 0 ? 1.0f : -1.0f;
        Vector3 center = _player.GlobalPosition
        + new Vector3(skill.AttackRangeOffset.X * facingDir, skill.AttackRangeOffset.Y, skill.AttackRangeOffset.Z)
        + new Vector3(facingDir * size.X * 0.5f, size.Y * 0.5f, 0);

        var spaceState = _player.GetWorld3D().DirectSpaceState;

        // 부위 판정은 Mob 본체(layer 4)가 아니라 Head/Leg/Body Area3D(layer 16)를 대상으로 해야 한다.
        var bodyQuery = new PhysicsShapeQueryParameters3D
        {
            Shape = new BoxShape3D { Size = size },
            Transform = new Transform3D(Basis.Identity, center),
            CollisionMask = 16,
            CollideWithBodies = false,
            CollideWithAreas = true,
        };

        DrawDebugHitbox(center, size);

        var found = new Dictionary<Mob, string>();
        foreach (var hit in spaceState.IntersectShape(bodyQuery))
        {
            if (hit["collider"].As<Node>() is not Area3D area) continue;
            if (area.GetParent() is not Mob mob) continue;

            string areaName = area.Name.ToString();
            string part = areaName is "Head" or "Leg" ? areaName : "Body";
            found[mob] = found.TryGetValue(mob, out string existing) ? PickBestPart(existing, part) : part;
        }

        foreach (var (mob, part) in found)
        {
            if (targets.Count >= skill.TargetCount) break;
            targets.Add((mob, part));
        }

        return targets;
    }

    // DebugDrawVirtualHitbox가 켜져있으면 가상 히트박스를 반투명 박스로 잠깐 표시한다.
    private void DrawDebugHitbox(Vector3 center, Vector3 size)
    {
        if (!DebugDrawVirtualHitbox) return;

        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0f, 0f, 0.35f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        GetTree().CurrentScene.AddChild(mesh);
        mesh.GlobalPosition = center;
        GetTree().CreateTimer(0.2f).Timeout += mesh.QueueFree;
    }

    // 5. 소환수형
    private void ExecuteSummon(SkillData skill, int level)
    {
        if (skill.SummonPrefab == null) return;

        var summon = skill.SummonPrefab.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(summon);
        summon.GlobalPosition = _player.GlobalPosition + new Vector3(-1.0f, 1.5f, 0);

        // if (summon is SummonPet pet)
        // {
        //     pet.Initialize(_player, skill.GetDuration(level));
        // }
    }
}