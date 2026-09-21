using Godot;

public enum ItemType
{
    Consumable,
    Equipment,
    Etc,
}

public enum EquipSlot
{
    None,
    Hat,
    Top,
    Bottom,
    Weapon,
    Shoes
}

[GlobalClass]
public partial class ItemData : Resource
{
    [ExportGroup("1. 기본 메타데이터")]
    [Export] public string ItemId = "red_potion";
    [Export] public string ItemName = "빨간 포션";
    [Export(PropertyHint.MultilineText)] public string Description = "HP를 즉시 회복시켜주는 포션.";
    [Export] public Texture2D Icon;
    [Export] public PackedScene Item3DModelScene;
    [Export] public ItemType Type = ItemType.Consumable;

    [ExportGroup("2. 소비 효과")]
    [Export] public int HealHp = 0;
    [Export] public int HealMp = 0;
    [Export] public float Cooldown = 1.0f;
    [Export] public bool ConsumeOnUse = true;

    [ExportGroup("3. 장비 효과")]
    [Export] public EquipSlot Slot = EquipSlot.None;
    [Export] public PackedScene EquipModelScene; // 장착 시 부착할 시각 모델 (Weapon은 왼손에 부착)
    [Export] public float AttackRange = 1.0f; // Weapon 전용: 스킬 AttackRangeW에 곱해지는 배율 (창은 길게, 단검은 짧게)
    [Export] public int BonusStr = 0;
    [Export] public int BonusDex = 0;
    [Export] public int BonusInt = 0;
    [Export] public int BonusLuk = 0;
    [Export] public int BonusDefense = 0;
    [Export] public int BonusMaxHp = 0;
    [Export] public int BonusMaxMp = 0;

    public ItemData() { }
}
