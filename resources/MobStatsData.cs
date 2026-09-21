using Godot;

[GlobalClass]
public partial class MobStatsData : Resource
{
    [ExportGroup("기본 정보")]
    [Export] public string MonsterId = "life";
    [Export] public string MonsterName = "라이프";
    [Export] public PackedScene MobScene;
    [ExportGroup("기본 능력치")]
    [Export] public int MaxHp = 50;
    [Export] public int MaxMp = 50;
    [Export] public int Defense = 5;
    [Export] public int AttackPower = 10; // 플레이어 접촉 시 입히는 대미지
    [Export] public float MoveSpeed = 5.0f;
    [Export] public float JumpVelocity = 0.0f;
    [Export] public float Acceleration = 40.0f;
    [Export] public float Deceleration = 40.0f;

    [ExportGroup("처치 보상")]
    [Export] public int RewardExp = 10;
    [Export] public int RewardMinMoney = 5;
    [Export] public int RewardMaxMoney = 15;

    [ExportGroup("드랍 테이블")]
    [Export] public Godot.Collections.Array<ItemDropData> DropItems = new();
}