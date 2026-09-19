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
    [Export] public int AttackPower = 10;

    public override void _Ready()
    {
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;
    }

    // 방어력 적용 후 실제로 깎인 대미지를 반환한다 (UI 표시용).
    public virtual int TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - Defense);
        CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);

        OnHpChanged?.Invoke();

        if (CurrentHp <= 0)
        {
            OnDied?.Invoke();
        }

        return actualDamage;
    }

    public virtual void ConsumeMp(int amount)
    {
        CurrentMp = Mathf.Max(0, CurrentMp - amount);
        OnStatsChanged?.Invoke();
    }


    public virtual void ConsumeHp(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged?.Invoke();
    }

    public virtual void RestoreMp(int amount)
    {
        CurrentMp = Mathf.Min(MaxMp, CurrentMp + amount);
        OnStatsChanged?.Invoke();
    }

    public virtual void RestoreHp(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        OnHpChanged?.Invoke();
    }
}