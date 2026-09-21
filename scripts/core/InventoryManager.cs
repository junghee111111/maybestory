using Godot;
using System;
using System.Collections.Generic;

// 스택 가능한(소비/기타) 아이템 한 종류의 보관 정보.
public class InventoryStack
{
    public ItemData Item { get; }
    public int Count { get; set; }

    public InventoryStack(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }
}

// 플레이어 인벤토리를 총괄하는 싱글톤.
// 장비는 같은 아이템이라도 슬롯을 공유하지 않고 각각 별도 슬롯을 차지하며,
// 소비/기타 아이템은 ItemId 기준으로 하나로 합쳐져 개수로 관리된다.
// 씬 트리 어딘가(예: GameWorld)에 한 번만 배치하거나, Project Settings > Autoload 에 등록해서 사용한다.
public partial class InventoryManager : Node
{
    public static InventoryManager Instance { get; private set; }

    [ExportGroup("Capacity")]
    [Export] public int EquipmentCapacity = 24;
    [Export] public int MaxStackSize = 99;

    // 장비: 인덱스 = 슬롯. 비어있으면 null.
    private readonly List<ItemData> _equipmentSlots = new();
    // 소비/기타: ItemId -> 스택 정보.
    private readonly Dictionary<string, InventoryStack> _consumables = new();
    private readonly Dictionary<string, InventoryStack> _etcItems = new();

    // 장비 슬롯 내용이 바뀌었을 때 (슬롯 인덱스, 아이템(null이면 비워짐)).
    public event Action<int, ItemData> OnEquipmentSlotChanged;
    // 소비/기타 스택 수량이 바뀌었을 때 (아이템 타입, 아이템, 개수(0이면 제거됨)).
    public event Action<ItemType, ItemData, int> OnStackChanged;

    public override void _Ready()
    {
        Instance = this;

        for (int i = 0; i < EquipmentCapacity; i++)
        {
            _equipmentSlots.Add(null);
        }
    }

    // ==========================================
    // 조회
    // ==========================================
    public IReadOnlyList<ItemData> EquipmentSlots => _equipmentSlots;

    public ItemData GetEquipmentSlot(int slotIndex)
    {
        return IsValidEquipmentIndex(slotIndex) ? _equipmentSlots[slotIndex] : null;
    }

    public IEnumerable<InventoryStack> GetConsumables() => _consumables.Values;

    public IEnumerable<InventoryStack> GetEtcItems() => _etcItems.Values;

    public int GetStackCount(ItemData item)
    {
        var stacks = GetStackDictionary(item?.Type);
        return stacks != null && stacks.TryGetValue(item.ItemId, out var stack) ? stack.Count : 0;
    }

    // ==========================================
    // 추가
    // ==========================================
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
        {
            return false;
        }

        return item.Type switch
        {
            ItemType.Equipment => AddEquipment(item, quantity),
            _ => AddToStack(item, quantity),
        };
    }

    // 장비는 같은 아이템이어도 항상 새 슬롯을 차지한다.
    private bool AddEquipment(ItemData item, int quantity)
    {
        bool addedAny = false;
        for (int i = 0; i < quantity; i++)
        {
            int slotIndex = _equipmentSlots.IndexOf(null);
            if (slotIndex < 0)
            {
                GD.PrintErr($"[InventoryManager] Equipment inventory is full, cannot add '{item.ItemName}'.");
                break;
            }

            _equipmentSlots[slotIndex] = item;
            OnEquipmentSlotChanged?.Invoke(slotIndex, item);
            addedAny = true;
        }

        return addedAny;
    }

    // 소비/기타는 ItemId 기준으로 합쳐서 개수만 늘린다.
    private bool AddToStack(ItemData item, int quantity)
    {
        var stacks = GetStackDictionary(item.Type);
        if (stacks == null)
        {
            return false;
        }

        if (!stacks.TryGetValue(item.ItemId, out var stack))
        {
            stack = new InventoryStack(item, 0);
            stacks[item.ItemId] = stack;
        }

        stack.Count = Mathf.Min(stack.Count + quantity, MaxStackSize);
        OnStackChanged?.Invoke(item.Type, item, stack.Count);
        return true;
    }

    // ==========================================
    // 제거
    // ==========================================
    public ItemData RemoveEquipment(int slotIndex)
    {
        if (!IsValidEquipmentIndex(slotIndex) || _equipmentSlots[slotIndex] == null)
        {
            return null;
        }

        ItemData removed = _equipmentSlots[slotIndex];
        _equipmentSlots[slotIndex] = null;
        OnEquipmentSlotChanged?.Invoke(slotIndex, null);
        return removed;
    }

    public bool RemoveFromStack(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
        {
            return false;
        }

        var stacks = GetStackDictionary(item.Type);
        if (stacks == null || !stacks.TryGetValue(item.ItemId, out var stack) || stack.Count < quantity)
        {
            return false;
        }

        stack.Count -= quantity;
        if (stack.Count <= 0)
        {
            stacks.Remove(item.ItemId);
            OnStackChanged?.Invoke(item.Type, item, 0);
        }
        else
        {
            OnStackChanged?.Invoke(item.Type, item, stack.Count);
        }

        return true;
    }

    private Dictionary<string, InventoryStack> GetStackDictionary(ItemType? type)
    {
        return type switch
        {
            ItemType.Consumable => _consumables,
            ItemType.Etc => _etcItems,
            _ => null,
        };
    }

    private bool IsValidEquipmentIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _equipmentSlots.Count;
    }
}
