using System;
using Godot;

// 최상위 몬스터
public abstract partial class Mob : Life<MobStats>
{
    public MobStatsData MobData; // 인스펙터에서 .tres 할당
    [Export] public NodePath VisualModelPath = "Model";
    [Export] public NodePath AnimationPlayerPath = "Model/AnimationPlayer";

    private static readonly PackedScene ItemDropScene = GD.Load<PackedScene>("res://item/ItemDrop.tscn");
    private const float CoinDropChance = 0.6f;

    private AnimationPlayer _animPlayer;
    private Node3D _visualModel;
    private CollisionShape3D _MobVolumeCollision;
    private Area3D _MobHurtboxArea;
    private float _direction = 1.0f; // 1 = 우측, -1 = 좌측
    private float _stateTimer = 0.0f;

    public event Action OnDeathToSpawner;

    protected Mob()
    {
        IsAttackable = true;
    }

    public override void _Ready()
    {
        base._Ready();
        _visualModel = GetNode<Node3D>(VisualModelPath);
        _animPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        _MobVolumeCollision = GetNode<CollisionShape3D>("CollisionShape3D");
        _MobHurtboxArea = GetNode<Area3D>("Area3D");

        if (MobData != null && Stats != null)
        {
            Stats.MaxHp = MobData.MaxHp;
            Stats.CurrentHp = MobData.MaxHp;
            Stats.MaxMp = MobData.MaxMp;
            Stats.CurrentMp = MobData.MaxMp;
            Stats.Defense = MobData.Defense;
            Stats.AttackPower = MobData.AttackPower;

            Speed = MobData.MoveSpeed;
            JumpVelocity = MobData.JumpVelocity;
            Acceleration = MobData.Acceleration;
            Deceleration = MobData.Deceleration;

            Stats.LifeName = MobData.MonsterName;
        }

        _animPlayer.AnimationFinished += OnAnimationFinished;
        ChooseNextAction();

        AddToGroup("Mob");
    }

    public void Initialize(MobStatsData data)
    {
        MobData = data;
    }

    // 부위별(Head/Leg/Body) 히트박스로부터 들어온 대미지에 배율을 적용한다. 기본형(단일 히트박스)은 배율 없음.
    protected virtual float GetPartDamageMultiplier(string partName) => 1.0f;

    public void TakePartDamage(string partName, int[] damages, Vector3 hitSourcePosition, bool[] criticals, string subText = "")
    {
        float multiplier = GetPartDamageMultiplier(partName);
        if (!Mathf.IsEqualApprox(multiplier, 1.0f))
        {
            var scaled = new int[damages.Length];
            for (int i = 0; i < damages.Length; i++)
            {
                scaled[i] = Mathf.RoundToInt(damages[i] * multiplier);
            }
            damages = scaled;
        }

        TakeDamage(damages, hitSourcePosition, criticals, subText);
    }

    public override void TakeDamage(int[] damages, Vector3 hitSourcePosition, bool[] criticals, string subText = "")
    {
        base.TakeDamage(damages, hitSourcePosition, criticals, subText);

        if (IsDead) return;

        PlayAnim("Hit", 0.1f);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 vel = Velocity;

        // 중력
        if (!IsOnFloor())
        {
            vel.Y -= 20.0f * dt;
        }

        // 사망 시에도 Die 애니메이션이 끝날 때까지 넉백 속도가 관성대로 흘러가도록 물리 이동은 계속 처리한다.
        if (IsDead)
        {
            vel.Z = 0.0f;
            Velocity = vel;
            MoveAndSlide();
            return;
        }

        if (!IsStunned && !IsBusy)
        {
            // 메이플 특유의 배회 AI: 걷다가 서서 두리번거리기를 반복
            _stateTimer -= dt;
            if (_stateTimer <= 0)
            {
                ChooseNextAction();
            }

            if (IsMoving)
            {
                vel.X = _direction * Speed;
                PlayAnim("Walk", 0.15f);

                // 방향 회전
                _visualModel.RotationDegrees = new Vector3(0, _direction > 0 ? 90 : -90, 0);
            }
            else
            {
                vel.X = Mathf.MoveToward(vel.X, 0, 10.0f * dt);
                PlayAnim("Idle", 0.15f);
            }
        }
        else
        {
            // 피격 경직 중에는 미끄러지듯 감속
            vel.X = Mathf.MoveToward(vel.X, 0, 15.0f * dt);
        }

        vel.Z = 0.0f;
        Velocity = vel;
        MoveAndSlide();
        ClampPositionToMapBounds();

        // 횡스크롤 Z축 고정
        Vector3 pos = GlobalPosition;
        pos.Z = GlobalPosition.Z;
        GlobalPosition = pos;
    }

    private void PlayAnim(string animName, float blend)
    {
        if (_animPlayer.CurrentAnimation != animName && _animPlayer.HasAnimation(animName))
        {
            _animPlayer.Play(animName, blend);
        }
    }

    public override async void OnDeath()
    {
        // 1. 경험치 및 메소 드랍
        DropRewards();
        // 2. 사망 애니메이션 후 소멸
        PlayAnim("Die", 0.1f);
        IsDead = true;
        // Area3D disable
        _MobHurtboxArea.SetDeferred("monitoring", false);

        OnDeathToSpawner?.Invoke();

        // waiting for play anim die
        await ToSignal(_animPlayer, "animation_finished");
        QueueFree();
    }

    protected virtual void DropRewards()
    {
        if (MobData == null)
        {
            return;
        }

        PlayerStats.Instance?.AddExp(MobData.RewardExp);

        foreach (ItemDropData drop in MobData.DropItems)
        {
            if (drop?.Item == null || GD.Randf() > drop.DropRate)
            {
                continue;
            }

            SpawnDrop(drop.Item, 0, drop.Item.Item3DModelScene);
        }

        if (MapManager.Instance?.CoinModel != null && GD.Randf() <= CoinDropChance)
        {
            int coinCount = GD.RandRange(3, 5);
            for (int i = 0; i < coinCount; i++)
            {
                SpawnDrop(null, 1, MapManager.Instance.CoinModel);
            }
        }
    }

    // 몬스터 위치에서 X(좌우), Y(위) 방향으로 흩뿌려지는 드랍 아이템을 하나 스폰한다. item이 null이면 코인으로 취급한다.
    private void SpawnDrop(ItemData item, int coinAmount, PackedScene visualScene)
    {
        if (visualScene == null || ItemDropScene == null)
        {
            return;
        }

        ItemDrop dropInstance = ItemDropScene.Instantiate<ItemDrop>();
        GetParent().AddChild(dropInstance);
        dropInstance.GlobalPosition = GlobalPosition;

        var initialVelocity = new Vector3((float)GD.RandRange(-7.0, 7.0), (float)GD.RandRange(10.0, 14.0), 0);
        dropInstance.Setup(item, coinAmount, visualScene, initialVelocity);
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Hit")
        {
            IsStunned = false;
            ChooseNextAction();
        }
        else if (animName == "Die")
        {
            QueueFree(); // 사망 애니메이션 끝나면 씬에서 완전 제거
        }
    }

    private void ChooseNextAction()
    {
        // 50% 확률로 대기 또는 걷기
        IsMoving = GD.Randf() > 0.5f;
        _stateTimer = (float)GD.RandRange(2.0, 4.0);

        if (IsMoving)
        {
            // 좌우 무작위 방향 선택
            _direction = GD.Randf() > 0.5f ? 1.0f : -1.0f;
        }
    }
}