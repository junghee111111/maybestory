using Godot;
using System.Collections.Generic;

// 열린 UI 창들을 연 순서대로 스택에 담아두고, ESC를 누르면 가장 최근에 연 창부터 닫아준다.
public partial class WindowManager : Node
{
    public static WindowManager Instance { get; private set; }

    private readonly List<UIWindowPanel> _openWindows = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel") || _openWindows.Count == 0)
        {
            return;
        }

        UIWindowPanel top = _openWindows[^1];
        top.Close();
        GetViewport().SetInputAsHandled();
    }

    public void NotifyOpened(UIWindowPanel window)
    {
        _openWindows.Remove(window);
        _openWindows.Add(window);
    }

    public void NotifyClosed(UIWindowPanel window)
    {
        _openWindows.Remove(window);
    }
}
