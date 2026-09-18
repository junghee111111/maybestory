using Godot;
using System;
using System.Collections.Generic;

public partial class SkillManager : Node
{
    [Export] public Godot.Collections.Array<SkillData> AllSkills = new(); // 에디터에서 .tres 등록

    private readonly Dictionary<string, SkillData> _skillDataDict = new();
    private readonly Dictionary<string, int> _allocatedPoints = new(); // SkillId -> Level
    private readonly Dictionary<string, float> _activeBuffTimers = new(); // SkillId -> 남은 시간

    private Player _player;
    private PlayerStats _stats;
    private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        _player = GetOwner<Player>();
        _stats = _player.GetNode<PlayerStats>("PlayerStats");
        _animPlayer = _player.GetNode<AnimationPlayer>("BaseChar/AnimationPlayer");

        foreach (var data in AllSkills)
        {
            _skillDataDict[data.SkillId] = data;
            _allocatedPoints[data.SkillId] = 0; // 초기 스킬 레벨 0
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

        // MP 체크 및 소모
        int mpCost = skill.GetMpCost(level);
        if (_stats.CurrentMp < mpCost)
        {
            GD.Print("[Skill] MP가 부족합니다.");
            return false;
        }
        _stats.CurrentMp -= mpCost;

        // 캐스팅 모션 재생
        if (!string.IsNullOrEmpty(skill.CastAnimationName) && _animPlayer.HasAnimation(skill.CastAnimationName))
        {
            _animPlayer.Play(skill.CastAnimationName, 0.1f);
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
        int calculatedDmg = Mathf.RoundToInt((_stats.Int * 4 + _stats.Luk) * skill.GetDamageMultiplier(level));
        if (projInstance is Projectile proj)
        {
            proj.Initialize(new Vector3(facingDir, 0, 0), skill.ProjectileSpeed, calculatedDmg);
        }
    }

    // 4. 자동 타게팅형 공격 (범위 내 몬스터 검색 및 VFX 생성)
    private void ExecuteAutoTarget(SkillData skill, int level)
    {
        // "Monsters" 그룹에서 사거리 내 가장 가까운 적 검색
        var monsters = GetTree().GetNodesInGroup("Mob");
        Node3D closestMonster = null;
        float minDist = skill.AttackRange;

        foreach (Node node in monsters)
        {
            if (node is Node3D m)
            {
                float dist = _player.GlobalPosition.DistanceTo(m.GlobalPosition);
                if (dist <= minDist)
                {
                    minDist = dist;
                    closestMonster = m;
                }
            }
        }

        if (closestMonster != null && skill.HitVfxScene != null)
        {
            var vfx = skill.HitVfxScene.Instantiate<Node3D>();
            GetTree().CurrentScene.AddChild(vfx);
            // 몬스터 위치 + 상대 오프셋에 생성
            vfx.GlobalPosition = closestMonster.GlobalPosition + skill.TargetOffset;

            int dmg = Mathf.RoundToInt((_stats.Int * 4 + _stats.Luk) * skill.GetDamageMultiplier(level));
            if (closestMonster.HasMethod("TakeDamage"))
            {
                closestMonster.Call("TakeDamage", dmg);
            }
            GD.Print($"[Target Attack] {closestMonster.Name}에게 {dmg} 대미지 적용");
        }
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