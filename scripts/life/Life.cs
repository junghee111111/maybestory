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
    protected bool IsDead = false;
    protected bool IsStunned = false;
    protected bool IsMoving = false;

    protected TStats Stats;

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

    public virtual void TakeDamage(int damage, Vector3 hitSourcePosition)
    {
        Stats?.TakeDamage(damage);
    }

    public virtual void HealHp(int amount)
    {
        Stats?.HealHp(amount);
    }

    public abstract void OnDeath();
}
