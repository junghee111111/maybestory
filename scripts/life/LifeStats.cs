using Godot;
using System;

[GlobalClass]
public partial class LifeStats : Node
{
    public event Action OnHpChanged;
    public event Action OnDied;
    public event Action OnStatsChanged;

    [ExportGroup("기본 정보")]
    [Export] public int Level = 1;
    [Export] public string LifeName = "Life";

    [ExportGroup("기본 생명력/자원")]
    [Export] public int MaxHp = 100;
    [Export] public int CurrentHp = 100;
    [Export] public int MaxMp = 50;
    [Export] public int CurrentMp = 50;

    [ExportGroup("4대 주스탯 & 방어력")]
    [Export] public int Str = 4;
    [Export] public int Dex = 4;
    [Export] public int Int = 4;
    [Export] public int Luk = 4;
    [Export] public int Defense = 10;

    public override void _Ready()
    {
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;
    }

    public virtual void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - Defense);
        CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);

        OnHpChanged?.Invoke();

        if (CurrentHp <= 0)
        {
            OnDied?.Invoke();
        }
    }

    public virtual void HealHp(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        OnHpChanged?.Invoke();
    }
}