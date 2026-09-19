using Godot;

// 맵 내 배치용 텔레포터. 위에서 위쪽 방향키를 누르면 TargetPosition으로 즉시 옮긴다.
// 카메라는 별도 처리 없이 CameraController의 기존 추적(Lerp) 로직이 그대로 부드럽게 따라온다.
public partial class Teleporter : Area3D
{
  [Export] public Vector3 TargetPosition = Vector3.Zero;
  [Export] public float Cooldown = 0.5f;

  private Player _playerInside;
  private bool _onCooldown = false;

  public override void _Ready()
  {
    BodyEntered += OnBodyEntered;
    BodyExited += OnBodyExited;
  }

  public override void _Process(double delta)
  {
    if (_onCooldown || _playerInside == null) return;

    if (Input.IsActionJustPressed("move_up"))
    {
      _playerInside.GlobalPosition = TargetPosition;
      _playerInside.Velocity = Vector3.Zero;

      _onCooldown = true;
      GetTree().CreateTimer(Cooldown).Timeout += () => _onCooldown = false;
    }
  }

  private void OnBodyEntered(Node3D body)
  {
    if (body is Player player)
    {
      _playerInside = player;
    }
  }

  private void OnBodyExited(Node3D body)
  {
    if (body == _playerInside)
    {
      _playerInside = null;
    }
  }
}

