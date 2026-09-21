using Godot;
using System;

[GlobalClass]
public partial class PlayerStats : LifeStats
{
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

    public void AddMoney(int amount)
    {
        _money = Mathf.Max(0, _money + amount);
        OnMoneyChanged?.Invoke();
    }

    public void AddExp(int amount)
    {
        CurrentExp += amount;
        while (CurrentExp >= MaxExp)
        {
            CurrentExp -= MaxExp;
            LevelUp();
        }
        OnExpChanged?.Invoke();
    }

    private void LevelUp()
    {
        Level++;
        StatPoints += 5;
        SkillPoints += 3;

        // 레벨업 시 HP/MP 증가 공식
        int hpGain = (int)GD.RandRange(12, 16);
        int mpGain = (int)GD.RandRange(10, 14) + Mathf.FloorToInt(Int * 0.1f);

        MaxHp += hpGain;
        MaxMp += mpGain;
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;

        MaxExp = Mathf.RoundToInt(MaxExp * 1.25f);
        OnLevelUp?.Invoke(Level);
    }
}