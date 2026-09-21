using Godot;
using System.Collections.Generic;

// 인벤토리 패널: 장비(슬롯 고정) + 소비/기타(아이템별 스택) 를 그려주고 I키로 열고 닫는다.
public partial class InventoryUI : Control
{
    [Export] public NodePath EquipmentGridPath = "Panel/VBoxContainer/EquipmentGrid";
    [Export] public NodePath ConsumableGridPath = "Panel/VBoxContainer/ConsumableGrid";
    [Export] public NodePath EtcGridPath = "Panel/VBoxContainer/EtcGrid";
    [Export] public PackedScene SlotScene;

    private GridContainer _equipmentGrid;
    private GridContainer _consumableGrid;
    private GridContainer _etcGrid;

    private readonly List<InventorySlotUI> _equipmentSlotNodes = new();
    private readonly Dictionary<string, InventorySlotUI> _consumableSlotNodes = new();
    private readonly Dictionary<string, InventorySlotUI> _etcSlotNodes = new();

    public override void _Ready()
    {
        Visible = false;

        _equipmentGrid = GetNode<GridContainer>(EquipmentGridPath);
        _consumableGrid = GetNode<GridContainer>(ConsumableGridPath);
        _etcGrid = GetNode<GridContainer>(EtcGridPath);

        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            GD.PrintErr("[InventoryUI] InventoryManager를 찾을 수 없습니다.");
            return;
        }

        BuildEquipmentSlots(inventory);
        foreach (InventoryStack stack in inventory.GetConsumables())
        {
            CreateOrUpdateStack(_consumableGrid, _consumableSlotNodes, stack.Item, stack.Count);
        }
        foreach (InventoryStack stack in inventory.GetEtcItems())
        {
            CreateOrUpdateStack(_etcGrid, _etcSlotNodes, stack.Item, stack.Count);
        }

        inventory.OnEquipmentSlotChanged += OnEquipmentSlotChanged;
        inventory.OnStackChanged += OnStackChanged;

        if (KeyboardManager.Instance != null)
        {
            KeyboardManager.Instance.OnSlotActivated += OnHotkeyActivated;
        }
    }

    public override void _ExitTree()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentSlotChanged -= OnEquipmentSlotChanged;
            InventoryManager.Instance.OnStackChanged -= OnStackChanged;
        }

        if (KeyboardManager.Instance != null)
        {
            KeyboardManager.Instance.OnSlotActivated -= OnHotkeyActivated;
        }
    }

    private void OnHotkeyActivated(HotkeySlot slot, SlotBinding binding)
    {
        if (binding.ContentType != SlotContentType.UI || binding.ContentId != "UI_INVENTORY")
        {
            return;
        }

        Visible = !Visible;
    }

    private void BuildEquipmentSlots(InventoryManager inventory)
    {
        for (int i = 0; i < inventory.EquipmentSlots.Count; i++)
        {
            InventorySlotUI slotNode = SlotScene.Instantiate<InventorySlotUI>();
            _equipmentGrid.AddChild(slotNode);
            _equipmentSlotNodes.Add(slotNode);
            slotNode.SetItem(inventory.EquipmentSlots[i]);
        }
    }

    private void OnEquipmentSlotChanged(int slotIndex, ItemData item)
    {
        if (slotIndex < 0 || slotIndex >= _equipmentSlotNodes.Count)
        {
            return;
        }

        _equipmentSlotNodes[slotIndex].SetItem(item);
    }

    private void OnStackChanged(ItemType type, ItemData item, int count)
    {
        (GridContainer grid, Dictionary<string, InventorySlotUI> slots) = type switch
        {
            ItemType.Consumable => (_consumableGrid, _consumableSlotNodes),
            ItemType.Etc => (_etcGrid, _etcSlotNodes),
            _ => (null, null),
        };

        if (grid == null)
        {
            return;
        }

        if (count <= 0)
        {
            RemoveStack(slots, item.ItemId);
        }
        else
        {
            CreateOrUpdateStack(grid, slots, item, count);
        }
    }

    private void CreateOrUpdateStack(GridContainer grid, Dictionary<string, InventorySlotUI> slots, ItemData item, int count)
    {
        if (!slots.TryGetValue(item.ItemId, out InventorySlotUI slotNode))
        {
            slotNode = SlotScene.Instantiate<InventorySlotUI>();
            grid.AddChild(slotNode);
            slots[item.ItemId] = slotNode;
        }

        slotNode.SetItem(item, count);
    }

    private void RemoveStack(Dictionary<string, InventorySlotUI> slots, string itemId)
    {
        if (!slots.TryGetValue(itemId, out InventorySlotUI slotNode))
        {
            return;
        }

        slots.Remove(itemId);
        slotNode.QueueFree();
    }
}
