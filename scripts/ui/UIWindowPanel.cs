using Godot;

// UI_* 콘텐츠 ID에 대응하는 토글형 창의 공통 베이스. WindowManager와 연동해 ESC로 최근에 연 순서대로 닫히는 스택에 합류한다.
public partial class UIWindowPanel : Control
{
    [Export] public string WindowContentId = "";

    public override void _Ready()
    {
        Visible = false;

        if (KeyboardManager.Instance != null)
        {
            KeyboardManager.Instance.OnSlotActivated += OnHotkeyActivated;
        }
    }

    public override void _ExitTree()
    {
        if (KeyboardManager.Instance != null)
        {
            KeyboardManager.Instance.OnSlotActivated -= OnHotkeyActivated;
        }
    }

    private void OnHotkeyActivated(HotkeySlot slot, SlotBinding binding)
    {
        if (binding.ContentType != SlotContentType.UI || binding.ContentId != WindowContentId)
        {
            return;
        }

        Toggle();
    }

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    public void Open()
    {
        Visible = true;
        WindowManager.Instance?.NotifyOpened(this);
        OnOpened();
    }

    public void Close()
    {
        Visible = false;
        WindowManager.Instance?.NotifyClosed(this);
        OnClosed();
    }

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
}
