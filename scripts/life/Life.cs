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

    public virtual void TakeDamage(int damage, Vector3 hitSourcePosition, bool isCritical = false, string subText = "")
    {
        if (!IsAttackable || IsDead || Stats == null) return;

        int actualDamage = Stats.TakeDamage(damage);
        DamageIndicator.Spawn(GetTree().CurrentScene, GlobalPosition + DamageIndicatorOffset, actualDamage, isCritical, subText);

        // 즉사 타격이어도 넉백은 적용되어야 하므로 IsDead 체크보다 먼저 계산한다.
        float damagePercent = Stats.MaxHp > 0 ? (float)damage / Stats.MaxHp * 100f : 0f;
        if (damagePercent >= KnockbackThresholdPercentage)
        {
            float knockDir = GlobalPosition.X >= hitSourcePosition.X ? 1.0f : -1.0f;
            Velocity = new Vector3(knockDir * KnockbackHorizontalForce, KnockbackVerticalForce, Velocity.Z);
        }

        if (IsDead) return;

        IsBusy = true;
        GetTree().CreateTimer(HitBusyDuration).Timeout += () => IsBusy = false;
    }

    // public virtual void HealHp(int amount)
    // {
    //     Stats?.HealHp(amount);
    // }

    public abstract void OnDeath();
}
