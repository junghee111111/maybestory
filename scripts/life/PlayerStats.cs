using Godot;
using System;

public enum StatKind
{
    Str,
    Dex,
    Int,
    Luk,
}

[GlobalClass]
public partial class PlayerStats : LifeStats
{
    public static PlayerStats Instance { get; private set; }

    public event Action<int> OnLevelUp;
    public event Action OnExpChanged;
    public event Action OnMoneyChanged;

    [ExportGroup("플레이어 성장")]
    private int _money = 0;
    [Export] public int CurrentExp = 0;
    [Export] public int MaxExp = 15;
    [Export] public int StatPoints = 0;   // AP
    [Export] public int SkillPoints = 0;  // SP
    [Export] public JobType CurrentJob = JobType.Beginner;

    public int Money => _money;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        MaxExp = GameManager.Instance?.GetMaxExp(Level) ?? MaxExp;
    }

    public void AddMoney(int amount)
    {
        _money = Mathf.Max(0, _money + amount);
        OnMoneyChanged?.Invoke();
    }

    // 스탯 포인트(AP) 1개를 소모해 원하는 스탯을 1 올린다. StatUI의 + 버튼에서 사용.
    public bool AllocateStatPoint(StatKind kind)
    {
        if (StatPoints <= 0) return false;

        switch (kind)
        {
            case StatKind.Str: Str++; break;
            case StatKind.Dex: Dex++; break;
            case StatKind.Int: Int++; break;
            case StatKind.Luk: Luk++; break;
        }

        StatPoints--;
        RaiseStatsChanged();
        return true;
    }

    public void AddExp(int amount)
    {
        if (Level >= GameManager.MaxLevel) return; // 만렙에서는 경험치를 더 얻지 않는다.

        CurrentExp += amount;

        while (Level < GameManager.MaxLevel && MaxExp > 0 && CurrentExp >= MaxExp)
        {
            LevelUp();
        }

        OnExpChanged?.Invoke();
    }

    // 사망 시 (40 - Luk/50*100)% 만큼 현재 경험치를 차감한다.
    public void ApplyDeathExpPenalty()
    {
        float lossPercent = Mathf.Clamp(40f - (Luk / 50f * 100f), 0f, 100f);
        int loss = Mathf.RoundToInt(CurrentExp * (lossPercent / 100f));
        CurrentExp = Mathf.Max(0, CurrentExp - loss);
        OnExpChanged?.Invoke();
    }

    private void LevelUp()
    {
        Level++;
        StatPoints += 5;
        SkillPoints += 3;
        CurrentExp = 0;

        // 레벨업 시 HP/MP 증가 공식
        int hpGain = (int)GD.RandRange(12, 16);
        int mpGain = (int)GD.RandRange(10, 14) + Mathf.FloorToInt(Int * 0.1f);

        MaxHp += hpGain;
        MaxMp += mpGain;
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;

        MaxExp = GameManager.Instance?.GetMaxExp(Level) ?? MaxExp;
        GameManager.Instance?.PlayLevelUpVfx((GetParent() as Node3D)?.GlobalPosition ?? Vector3.Zero);

        OnLevelUp?.Invoke(Level);
    }
}