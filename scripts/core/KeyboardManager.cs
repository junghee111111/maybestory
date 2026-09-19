using Godot;
using System;
using System.Collections.Generic;

// A,S,D,F,Q,W,E 슬롯에 대한 키 리바인딩 + 스킬/아이템 배치를 총괄하는 싱글톤.
// 씬 트리 어딘가(예: GameWorld)에 한 번만 배치하거나, Project Settings > Autoload 에 등록해서 사용한다.
public partial class KeyboardManager : Node
{
    public static KeyboardManager Instance { get; private set; }

    private static readonly HotkeySlot[] AllSlots =
    {
        HotkeySlot.A, HotkeySlot.S, HotkeySlot.D, HotkeySlot.F,
        HotkeySlot.Q, HotkeySlot.W, HotkeySlot.E, HotkeySlot.R,
        HotkeySlot.Num1, HotkeySlot.Num2, HotkeySlot.Num3, HotkeySlot.Num4,
    };

    private static readonly Dictionary<HotkeySlot, Key> DefaultKeys = new()
    {
        { HotkeySlot.A, Key.A },
        { HotkeySlot.S, Key.S },
        { HotkeySlot.D, Key.D },
        { HotkeySlot.F, Key.F },
        { HotkeySlot.Q, Key.Q },
        { HotkeySlot.W, Key.W },
        { HotkeySlot.E, Key.E },
        { HotkeySlot.R, Key.R },
        { HotkeySlot.Num1, Key.Key1 },
        { HotkeySlot.Num2, Key.Key2 },
        { HotkeySlot.Num3, Key.Key3 },
        { HotkeySlot.Num4, Key.Key4 },
    };

    // 기본 슬롯 배치: A=공격, D=점프, F=줍기.
    private static readonly Dictionary<HotkeySlot, SlotBinding> DefaultBindings = new()
    {
        { HotkeySlot.A, new SlotBinding(SlotContentType.Skill, "SkillAttack") },
        { HotkeySlot.D, new SlotBinding(SlotContentType.Skill, "SkillJump") },
        { HotkeySlot.F, new SlotBinding(SlotContentType.Skill, "SkillPickUp") },
    };

    private const string ConfigPath = "user://keybinds.cfg";

    private readonly Dictionary<HotkeySlot, Key> _boundKeys = new();
    private readonly Dictionary<HotkeySlot, SlotBinding> _slotBindings = new();

    // 슬롯의 키가 눌렸고, 그 슬롯에 스킬/아이템이 배치되어 있을 때 발생.
    public event Action<HotkeySlot, SlotBinding> OnSlotActivated;
    // 슬롯에 스킬/아이템이 새로 배치되거나 비워졌을 때 발생 (UI 갱신용).
    public event Action<HotkeySlot, SlotBinding> OnSlotAssigned;
    // 슬롯에 물린 물리 키가 바뀌었을 때 발생 (UI 갱신용).
    public event Action<HotkeySlot, Key> OnKeyRebound;

    public override void _Ready()
    {
        Instance = this;

        foreach (HotkeySlot slot in AllSlots)
        {
            _boundKeys[slot] = DefaultKeys[slot];
            _slotBindings[slot] = DefaultBindings.TryGetValue(slot, out var defaultBinding) ? defaultBinding : new SlotBinding();
            RegisterAction(slot, DefaultKeys[slot]);
        }

        LoadBindings();
    }

    public override void _Process(double delta)
    {
        foreach (HotkeySlot slot in AllSlots)
        {
            if (!Input.IsActionJustPressed(ActionName(slot)))
            {
                continue;
            }

            SlotBinding binding = _slotBindings[slot];
            if (!binding.IsEmpty)
            {
                OnSlotActivated?.Invoke(slot, binding);
            }
        }
    }

    // ==========================================
    // 슬롯 콘텐츠 배치 (스킬 / 아이템)
    // ==========================================
    public void AssignSkill(HotkeySlot slot, string skillId)
    {
        SetBinding(slot, new SlotBinding(SlotContentType.Skill, skillId));
    }

    public void AssignItem(HotkeySlot slot, string itemId)
    {
        SetBinding(slot, new SlotBinding(SlotContentType.Item, itemId));
    }

    public void ClearSlot(HotkeySlot slot)
    {
        SetBinding(slot, new SlotBinding());
    }

    public SlotBinding GetBinding(HotkeySlot slot) => _slotBindings[slot];

    private void SetBinding(HotkeySlot slot, SlotBinding binding)
    {
        _slotBindings[slot] = binding;
        OnSlotAssigned?.Invoke(slot, binding);
        SaveBindings();
    }

    // ==========================================
    // 키 리바인딩
    // ==========================================
    public Key GetBoundKey(HotkeySlot slot) => _boundKeys[slot];

    public bool RebindSlotKey(HotkeySlot slot, Key newKey)
    {
        foreach (var kvp in _boundKeys)
        {
            if (kvp.Key != slot && kvp.Value == newKey)
            {
                GD.PrintErr($"[KeyboardManager] Key '{newKey}' is already bound to slot '{kvp.Key}'.");
                return false;
            }
        }

        _boundKeys[slot] = newKey;
        RegisterAction(slot, newKey);
        OnKeyRebound?.Invoke(slot, newKey);
        SaveBindings();
        return true;
    }

    private static string ActionName(HotkeySlot slot) => $"hotkey_{slot.ToString().ToLowerInvariant()}";

    private static void RegisterAction(HotkeySlot slot, Key key)
    {
        string action = ActionName(slot);
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }
        else
        {
            InputMap.ActionEraseEvents(action);
        }

        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        GD.Print($"Registered action '{action}' with key '{key}'");
    }

    // ==========================================
    // 저장 / 불러오기 (user://keybinds.cfg)
    // ==========================================
    private void SaveBindings()
    {
        var config = new ConfigFile();
        foreach (HotkeySlot slot in AllSlots)
        {
            string section = slot.ToString();
            SlotBinding binding = _slotBindings[slot];
            config.SetValue(section, "key", (int)_boundKeys[slot]);
            config.SetValue(section, "content_type", (int)binding.ContentType);
            config.SetValue(section, "content_id", binding.ContentId);
        }
        config.Save(ConfigPath);
    }

    private void LoadBindings()
    {
        var config = new ConfigFile();
        if (config.Load(ConfigPath) != Error.Ok)
        {
            return;
        }

        foreach (HotkeySlot slot in AllSlots)
        {
            string section = slot.ToString();
            if (!config.HasSection(section))
            {
                continue;
            }

            if (config.HasSectionKey(section, "key"))
            {
                var key = (Key)(int)config.GetValue(section, "key");
                _boundKeys[slot] = key;
                RegisterAction(slot, key);
            }

            var contentType = (SlotContentType)(int)config.GetValue(section, "content_type", (int)SlotContentType.None);
            var contentId = (string)config.GetValue(section, "content_id", string.Empty);
            _slotBindings[slot] = new SlotBinding(contentType, contentId);
        }
    }
}
