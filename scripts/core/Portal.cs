using Godot;
using System.Threading.Tasks;

// 맵 내 배치용 포탈. Area3D 안에서 위쪽 방향키를 누르면 다음 맵의 지정 포탈 위치로 이동한다.
public partial class Portal : Area3D
{
  [Export] public string PortalId = "Portal0";
  [Export] public string NextMapScenePath = "";
  [Export] public string NextMapPortalId = "Default";

  private Player _playerInside;
  private bool _isTraveling = false;

  public override void _Ready()
  {
    BodyEntered += OnBodyEntered;
    BodyExited += OnBodyExited;
  }

  public override void _Process(double delta)
  {
    if (_isTraveling || _playerInside == null) return;

    if (Input.IsActionJustPressed("move_up"))
    {
      _ = EnterPortalAsync(_playerInside);
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

  private async Task EnterPortalAsync(Player player)
  {
    if (string.IsNullOrEmpty(NextMapScenePath) || MapLoader.Instance == null) return;

    _isTraveling = true;
    _playerInside = null;

    // 맵 전환 중 접촉 피격으로 죽는 것을 방지한다.
    player.SetHurtboxMonitoring(false);
    await MapLoader.Instance.TravelToPortal(NextMapScenePath, NextMapPortalId);
    player.SetHurtboxMonitoring(true);

    _isTraveling = false;
  }
}
