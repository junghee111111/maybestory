using Godot;

// 몬스터 드랍 테이블 한 줄: 아이템과 드랍 확률(0~1)을 짝지어 관리한다.
[GlobalClass]
public partial class ItemDropData : Resource
{
    [Export] public ItemData Item;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DropRate = 0.1f;

    public ItemDropData() { }
}
