using Godot;
using System;

public partial class HotKeyGridItem : TextureRect
{
	private TextureRect _icon;
	private Label _keyLabel;
	private HotkeySlot _slot;

	[Export] public string Key = "A";
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("KeyIcon");
		_keyLabel = GetNode<Label>("KeyLabel");

		_keyLabel.Text = Key;
		_slot = Enum.Parse<HotkeySlot>(Key);

		if (KeyboardManager.Instance != null)
		{
			GD.Print($"HotKeyGridItem: Subscribed to OnSlotAssigned for slot '{_slot}'");
			KeyboardManager.Instance.OnSlotAssigned += OnSlotAssigned;
			RefreshIcon(KeyboardManager.Instance.GetBinding(_slot));
		}
	}

	public override void _ExitTree()
	{
		if (KeyboardManager.Instance != null)
		{
			KeyboardManager.Instance.OnSlotAssigned -= OnSlotAssigned;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnSlotAssigned(HotkeySlot slot, SlotBinding binding)
	{
		if (slot != _slot) return;
		RefreshIcon(binding);
	}

	private void RefreshIcon(SlotBinding binding)
	{
		SkillData skill = binding.ContentType == SlotContentType.Skill
			? SkillManager.Instance?.GetSkillData(binding.ContentId)
			: null;
		if (skill != null)
		{
			_icon.Texture = skill?.Icon;
		}
		else
		{
			GD.PrintErr($"HotKeyGridItem: No skill found for binding '{binding.ContentId}'");
			_icon.Texture = null;
		}

	}
}
