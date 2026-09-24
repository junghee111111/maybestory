using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

// Autoload: 플레이어 진행 상태(스탯/인벤토리/스킬/현재 맵&위치)를 JSON으로 저장/불러오기한다.
public partial class SaveManager : Node
{
  public static SaveManager Instance { get; private set; }

  private const string SaveDir = "user://elinia/";
  private const string SaveFilePath = "user://elinia/savegame.json";
  public const string GameWorldScenePath = "res://common/GameWorld.tscn";

  // Load()로 읽어들인 뒤 GameWorld가 생성되면 ApplyPendingLoad()로 실제 게임 상태에 반영된다.
  private PlayerSaveData _pendingLoad;

  public override void _Ready()
  {
    Instance = this;
  }

  public bool HasSaveFile() => FileAccess.FileExists(SaveFilePath);

  public void DeleteSave()
  {
    if (HasSaveFile())
    {
      DirAccess.RemoveAbsolute(SaveFilePath);
    }
  }

  // 현재 게임 상태를 JSON으로 직렬화해 user:// 에 저장한다.
  public bool Save()
  {
    PlayerStats stats = PlayerStats.Instance;
    InventoryManager inventory = InventoryManager.Instance;
    Player player = GetTree().Root.GetNodeOrNull<Player>("GameWorld/LifeContainer/Player");
    if (stats == null || inventory == null || player == null)
    {
      GD.PrintErr("[SaveManager] Cannot save: game is not fully loaded.");
      return false;
    }

    SkillManager skillManager = player.GetNodeOrNull<SkillManager>("SkillManager");
    EquipManager equipManager = player.GetNodeOrNull<EquipManager>("EquipManager");

    var data = new PlayerSaveData
    {
      Level = stats.Level,
      CurrentExp = stats.CurrentExp,
      MaxExp = stats.MaxExp,
      StatPoints = stats.StatPoints,
      SkillPoints = stats.SkillPoints,
      CurrentJob = stats.CurrentJob.ToString(),
      Str = stats.Str,
      Dex = stats.Dex,
      Int = stats.Int,
      Luk = stats.Luk,
      Defense = stats.Defense,
      AttackPower = stats.AttackPower,
      MaxHp = stats.MaxHp,
      CurrentHp = stats.CurrentHp,
      MaxMp = stats.MaxMp,
      CurrentMp = stats.CurrentMp,
      Money = stats.Money,
      MapScenePath = MapManager.Instance?.CurrentMapScenePath ?? "res://map/Map0.tscn",
      PosX = player.GlobalPosition.X,
      PosY = player.GlobalPosition.Y,
      PosZ = player.GlobalPosition.Z,
    };

    foreach (ItemData item in inventory.EquipmentSlots)
    {
      data.EquipmentItemIds.Add(item?.ItemId ?? "");
    }
    foreach (InventoryStack stack in inventory.GetConsumables())
    {
      data.ConsumableCounts[stack.Item.ItemId] = stack.Count;
    }
    foreach (InventoryStack stack in inventory.GetEtcItems())
    {
      data.EtcCounts[stack.Item.ItemId] = stack.Count;
    }

    if (equipManager != null)
    {
      foreach (var (slot, item) in equipManager.GetAllEquipped())
      {
        if (item != null)
        {
          data.EquippedSlotItemIds[slot.ToString()] = item.ItemId;
        }
      }
    }

    if (skillManager != null)
    {
      foreach (var (skillId, level) in skillManager.GetAllAllocatedPoints())
      {
        data.AllocatedSkillPoints[skillId] = level;
      }
    }

    DirAccess.MakeDirRecursiveAbsolute(SaveDir);
    using FileAccess file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
    if (file == null)
    {
      GD.PrintErr($"[SaveManager] Failed to open save file for writing: {FileAccess.GetOpenError()}");
      return false;
    }

    file.StoreString(JsonSerializer.Serialize(data));
    GD.Print("[SaveManager] Game saved.");
    return true;
  }

  // 저장 파일을 읽어 _pendingLoad에 보관한다. GameWorld가 로드된 뒤 ApplyPendingLoad()를 호출해야 실제로 반영된다.
  public bool Load()
  {
    if (!HasSaveFile()) return false;

    using FileAccess file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
    if (file == null)
    {
      GD.PrintErr($"[SaveManager] Failed to open save file for reading: {FileAccess.GetOpenError()}");
      return false;
    }

    string json = file.GetAsText();
    try
    {
      _pendingLoad = JsonSerializer.Deserialize<PlayerSaveData>(json);
      return _pendingLoad != null;
    }
    catch (Exception e)
    {
      GD.PrintErr($"[SaveManager] Failed to parse save file: {e.Message}");
      _pendingLoad = null;
      return false;
    }
  }

  public bool HasPendingLoad => _pendingLoad != null;

  // GameWorld가 초기 스폰(Map0/Default)까지 마친 뒤 호출되어 저장된 상태로 덮어쓴다.
  public void ApplyPendingLoad()
  {
    if (_pendingLoad == null) return;
    PlayerSaveData data = _pendingLoad;
    _pendingLoad = null;

    PlayerStats stats = PlayerStats.Instance;
    InventoryManager inventory = InventoryManager.Instance;
    Player player = GetTree().Root.GetNodeOrNull<Player>("GameWorld/LifeContainer/Player");
    if (stats == null || inventory == null || player == null)
    {
      GD.PrintErr("[SaveManager] Cannot apply pending load: game is not fully loaded.");
      return;
    }

    // Player 씬 기본값으로 미리 장착된 장비가 있으면 해제한다. 저장된 절대 스탯값으로 곧 덮어쓸 것이므로
    // 이 시점에 해제해서 생기는 스탯 보너스 차감은 결과에 영향을 주지 않는다.
    EquipManager equipManager = player.GetNodeOrNull<EquipManager>("EquipManager");
    if (equipManager != null)
    {
      foreach (EquipSlot slot in Enum.GetValues<EquipSlot>())
      {
        equipManager.Unequip(slot);
      }
    }

    stats.Level = data.Level;
    stats.CurrentExp = data.CurrentExp;
    stats.MaxExp = data.MaxExp;
    stats.StatPoints = data.StatPoints;
    stats.SkillPoints = data.SkillPoints;
    if (Enum.TryParse(data.CurrentJob, out JobType job)) stats.CurrentJob = job;
    stats.Str = data.Str;
    stats.Dex = data.Dex;
    stats.Int = data.Int;
    stats.Luk = data.Luk;
    stats.Defense = data.Defense;
    stats.AttackPower = data.AttackPower;
    stats.MaxHp = data.MaxHp;
    stats.CurrentHp = data.CurrentHp;
    stats.MaxMp = data.MaxMp;
    stats.CurrentMp = data.CurrentMp;
    stats.AddMoney(data.Money);

    foreach (string itemId in data.EquipmentItemIds)
    {
      ItemData item = ItemDatabase.Find(itemId);
      if (item != null) inventory.AddItem(item, 1);
    }
    foreach (var (itemId, count) in data.ConsumableCounts)
    {
      ItemData item = ItemDatabase.Find(itemId);
      if (item != null) inventory.AddItem(item, count);
    }
    foreach (var (itemId, count) in data.EtcCounts)
    {
      ItemData item = ItemDatabase.Find(itemId);
      if (item != null) inventory.AddItem(item, count);
    }

    if (equipManager != null)
    {
      foreach (var (slotName, itemId) in data.EquippedSlotItemIds)
      {
        if (Enum.TryParse(slotName, out EquipSlot slot))
        {
          ItemData item = ItemDatabase.Find(itemId);
          equipManager.RestoreEquipped(slot, item);
        }
      }
    }

    SkillManager skillManager = player.GetNodeOrNull<SkillManager>("SkillManager");
    skillManager?.RestoreAllocatedPoints(data.AllocatedSkillPoints);

    if (data.MapScenePath != MapManager.Instance?.CurrentMapScenePath)
    {
      PackedScene mapScene = GD.Load<PackedScene>(data.MapScenePath);
      if (mapScene != null)
      {
        MapManager.Instance?.ChangeMap(mapScene);
      }
    }
    player.GlobalPosition = new Vector3(data.PosX, data.PosY, data.PosZ);
    player.Velocity = Vector3.Zero;

    GD.Print("[SaveManager] Save data applied.");
  }
}
