using Godot;
using System;

public abstract partial class Life<TStats> : CharacterBody3D where TStats : LifeStats
{
    [ExportGroup("Maybe :: Movement Settings")]
    [Export] public float Speed = 10.0f;
    [Export] public float JumpVelocity = 10.0f;
    [Export] public float Acceleration = 40.0f;
    [Export] public float Deceleration = 40.0f;
    [Export] public float DecelerationWhileAttacking = 15.0f;

    [ExportGroup("Maybe :: Node Settings")]
    [Export] public NodePath StatsPath = "LifeStats";

    [ExportGroup("Maybe :: Combat")]
    [Export] public bool IsAttackable = false; // NPC 등은 false로 두어 피격/넉백을 막는다.
    [Export] public Vector3 DamageIndicatorOffset = new Vector3(0, 2.2f, 0);
    [Export] public float KnockbackHorizontalForce = 12.0f;
    [Export] public float KnockbackVerticalForce = 8.0f;
    [Export] public float HitBusyDuration = 0.5f;
    [Export] public float KnockbackThresholdPercentage = 5.0f; // 이 비율(%) 이상의 대미지를 받으면 넉백

    protected bool IsDead = false;
    protected bool IsStunned = false;
    protected bool IsMoving = false;
    protected bool IsBusy = false;

    protected TStats Stats;
    private const float ComboHitInterval = 0.03f;

    public int AttackPower => Stats?.AttackPower ?? 0;

    public override void _Ready()
    {
        Stats = GetNodeOrNull<TStats>(StatsPath);
        if (Stats == null)
        {
            GD.PrintErr($"[{Name}] {typeof(TStats).Name} node not found at path '{StatsPath}'.");
            return;
        }

        Stats.OnDied += OnDeath;
    }

    public void TakeDamage(int damage, Vector3 hitSourcePosition, bool isCritical = false, string subText = "")
        => TakeDamage(new[] { damage }, hitSourcePosition, isCritical, subText);

    // damages 배열의 각 원소를 ComboHitInterval 간격으로 순차 적용하며 그때마다 DamageIndicator를 띄운다.
    public virtual void TakeDamage(int[] damages, Vector3 hitSourcePosition, bool isCritical = false, string subText = "")
    {
        if (!IsAttackable || IsDead || Stats == null || IsBusy || damages == null || damages.Length == 0) return;

        ApplyDamageSequence(damages, hitSourcePosition, isCritical, subText);
    }

    private async void ApplyDamageSequence(int[] damages, Vector3 hitSourcePosition, bool isCritical, string subText)
    {
        for (int i = 0; i < damages.Length; i++)
        {
            int actualDamage = Stats.TakeDamage(damages[i]);
            DamageIndicator.Spawn(GetTree().CurrentScene, GlobalPosition + DamageIndicatorOffset, actualDamage, isCritical, subText);

            // 즉사 타격이어도 넉백은 적용되어야 하므로 IsDead 체크보다 먼저 계산한다. (넉백은 최초 타격 기준 1회만 적용)
            if (i == 0)
            {
                float damagePercent = Stats.MaxHp > 0 ? (float)damages[i] / Stats.MaxHp * 100f : 0f;
                if (damagePercent >= KnockbackThresholdPercentage)
                {
                    float knockDir = GlobalPosition.X >= hitSourcePosition.X ? 1.0f : -1.0f;
                    Velocity = new Vector3(knockDir * KnockbackHorizontalForce, KnockbackVerticalForce, Velocity.Z);
                }
            }

            if (i < damages.Length - 1)
            {
                await ToSignal(GetTree().CreateTimer(ComboHitInterval), SceneTreeTimer.SignalName.Timeout);
            }
        }

        if (!IsDead)
        {
            IsBusy = true;
            GetTree().CreateTimer(HitBusyDuration).Timeout += () => IsBusy = false;
        }
    }

    // public virtual void HealHp(int amount)
    // {
    //     Stats?.HealHp(amount);
    // }

    public abstract void OnDeath();
}
