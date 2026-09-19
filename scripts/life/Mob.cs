using System;
using Godot;

// 최상위 몬스터
public abstract partial class Mob : Life<MobStats>
{
    public MobStatsData MobData; // 인스펙터에서 .tres 할당
    [Export] public NodePath VisualModelPath = "Model";
    [Export] public NodePath AnimationPlayerPath = "Model/AnimationPlayer";

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

    public override void TakeDamage(int damage, Vector3 hitSourcePosition, bool isCritical = false, string subText = "")
    {
        base.TakeDamage(damage, hitSourcePosition, isCritical, subText);

        if (IsDead) return;

        PlayAnim("Hit", 0.1f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead) return;

        float dt = (float)delta;
        Vector3 vel = Velocity;

        // 중력
        if (!IsOnFloor())
        {
            vel.Y -= 20.0f * dt;
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
        GD.Print($"[Mob] 경험치 {MobData.RewardExp} 지급 및 드랍 아이템 생성");
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