using Godot;
using System;
using System.Collections.Generic;

public partial class EquipManager : Node
{
	// BaseChar's root is a plain Node3D; the actual skeleton/bones live somewhere inside it.
	[Export] public NodePath SkeletonPath = "BaseChar";
	// Weapons are held in the left hand, so the visual is attached to this bone.
	[Export] public string LeftHandBoneName = "Hand.L";

	[ExportGroup("Maybe :: Current Equipment")]
	[Export] public ItemData WeaponSlot;
	[Export] public ItemData ShieldSlot;
	[Export] public ItemData CapSlot;
	[Export] public ItemData TopSlot;
	[Export] public ItemData BottomSlot;
	[Export] public ItemData ShoesSlot;
	[Export] public ItemData EarringsSlot;

	public event Action<EquipSlot, ItemData> OnEquipmentChanged;

	private Player _player;
	private PlayerStats _stats;
	private BoneAttachment3D _weaponAttach;
	private Node _weaponVisual;

	private readonly Dictionary<EquipSlot, ItemData> _equipped = new();

	// Weapon visual's hitbox (Area3D/HitArea), null if no weapon is equipped or it lacks one.
	public Area3D WeaponHitArea => (_weaponVisual as Node3D)?.GetNodeOrNull<Area3D>("Area3D");

	public override void _Ready()
	{
		_player = GetOwner<Player>();
		_stats = _player.GetNode<PlayerStats>("PlayerStats");
		_weaponAttach = CreateWeaponAttachment();
		if (WeaponSlot != null)
		{
			Equip(WeaponSlot);
		}
	}

	private BoneAttachment3D CreateWeaponAttachment()
	{
		var searchRoot = _player.GetNodeOrNull(SkeletonPath) ?? _player;
		var skeleton = FindChildOfType<Skeleton3D>(searchRoot);
		if (skeleton == null)
		{
			GD.PrintErr($"[EquipManager] Skeleton3D not found under '{SkeletonPath}'.");
			return null;
		}

		if (skeleton.FindBone(LeftHandBoneName) < 0)
		{
			GD.PrintErr($"[EquipManager] Bone '{LeftHandBoneName}' not found in '{skeleton.Name}'.");
			return null;
		}

		var attachment = new BoneAttachment3D { Name = "WeaponAttach_L", BoneName = LeftHandBoneName };
		skeleton.AddChild(attachment);
		return attachment;
	}

	private static T FindChildOfType<T>(Node root) where T : class
	{
		if (root is T match) return match;

		foreach (Node child in root.GetChildren())
		{
			var found = FindChildOfType<T>(child);
			if (found != null) return found;
		}

		return null;
	}

	public ItemData GetEquipped(EquipSlot slot)
	{
		return _equipped.TryGetValue(slot, out var item) ? item : null;
	}

	public IReadOnlyDictionary<EquipSlot, ItemData> GetAllEquipped() => _equipped;

	// 저장 파일 불러오기 전용: PlayerStats에 이미 장비 보너스가 반영된 값을 저장/복원하므로
	// 여기서는 스탯 보너스를 다시 더하지 않고 장착 상태(및 무기 비주얼)만 복원한다.
	public void RestoreEquipped(EquipSlot slot, ItemData item)
	{
		if (item == null) return;

		_equipped[slot] = item;
		if (slot == EquipSlot.Weapon)
		{
			AttachWeaponVisual(item);
		}

		OnEquipmentChanged?.Invoke(slot, item);
	}

	public bool Equip(ItemData item)
	{
		if (item == null || item.Type != ItemType.Equipment || item.Slot == EquipSlot.None)
		{
			GD.PrintErr($"[EquipManager] '{item?.ItemName}' is not a valid equipment item.");
			return false;
		}

		Unequip(item.Slot);

		_equipped[item.Slot] = item;
		ApplyStatBonus(item, 1);

		if (item.Slot == EquipSlot.Weapon)
		{
			AttachWeaponVisual(item);
		}

		OnEquipmentChanged?.Invoke(item.Slot, item);
		return true;
	}

	public void Unequip(EquipSlot slot)
	{
		if (!_equipped.TryGetValue(slot, out var previous))
		{
			return;
		}

		ApplyStatBonus(previous, -1);
		_equipped.Remove(slot);

		if (slot == EquipSlot.Weapon)
		{
			DetachWeaponVisual();
		}

		OnEquipmentChanged?.Invoke(slot, null);
	}

	private void ApplyStatBonus(ItemData item, int sign)
	{
		if (_stats == null) return;

		_stats.Str += sign * item.BonusStr;
		_stats.Dex += sign * item.BonusDex;
		_stats.Int += sign * item.BonusInt;
		_stats.Luk += sign * item.BonusLuk;
		_stats.Defense += sign * item.BonusDefense;
		_stats.MaxHp += sign * item.BonusMaxHp;
		_stats.MaxMp += sign * item.BonusMaxMp;
		_stats.CurrentHp = Mathf.Min(_stats.CurrentHp, _stats.MaxHp);
		_stats.CurrentMp = Mathf.Min(_stats.CurrentMp, _stats.MaxMp);
	}

	private void AttachWeaponVisual(ItemData item)
	{
		DetachWeaponVisual();

		if (item.EquipModelScene == null || _weaponAttach == null)
		{
			return;
		}

		_weaponVisual = item.EquipModelScene.Instantiate();
		_weaponAttach.AddChild(_weaponVisual);
	}

	private void DetachWeaponVisual()
	{
		if (_weaponVisual == null) return;

		_weaponVisual.QueueFree();
		_weaponVisual = null;
	}
}
