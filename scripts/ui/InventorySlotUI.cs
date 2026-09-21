using Godot;

// 인벤토리 슬롯 하나(장비 슬롯 또는 소비/기타 스택)를 표시하는 UI 조각.
public partial class InventorySlotUI : TextureRect
{
    private TextureRect _icon;
    private Label _countLabel;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _countLabel = GetNode<Label>("CountLabel");
    }

    // count가 1 이하면(장비 슬롯 등) 개수 라벨을 숨긴다.
    public void SetItem(ItemData item, int count = 1)
    {
        if (_icon == null)
        {
            return;
        }

        _icon.Texture = item?.Icon;
        _icon.Visible = item != null;
        _countLabel.Visible = item != null && count > 1;
        _countLabel.Text = count.ToString();
    }

    public void Clear()
    {
        SetItem(null);
    }
}
