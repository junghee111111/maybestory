using Godot;

// GameWorld 루트에 부착. 다른 매니저/플레이어가 모두 _Ready된 뒤 SaveManager의 대기 중인 불러오기 데이터를 적용한다.
public partial class GameWorldBootstrap : Node3D
{
  public override void _Ready()
  {
    if (SaveManager.Instance != null && SaveManager.Instance.HasPendingLoad)
    {
      CallDeferred(nameof(ApplyPendingLoad));
    }
  }

  private void ApplyPendingLoad()
  {
    SaveManager.Instance.ApplyPendingLoad();
  }

  // F5: 빠른 저장.
  public override void _UnhandledKeyInput(InputEvent @event)
  {
    if (@event is InputEventKey { Keycode: Key.F5, Pressed: true, Echo: false })
    {
      SaveManager.Instance?.Save();
    }
  }
}
