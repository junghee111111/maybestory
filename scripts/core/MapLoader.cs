using Godot;
using System.Threading.Tasks;

// Autoload: Portal에서 호출되어 화면 페이드아웃 → 다음 맵 비동기 로딩/스폰 → 페이드인을 담당한다.
public partial class MapLoader : CanvasLayer
{
  public static MapLoader Instance { get; private set; }

  [Export] public float FadeDuration = 0.4f;

  private ColorRect _fadeRect;

  public override void _Ready()
  {
    Instance = this;
    Layer = 128; // 항상 최상단에 그려지도록

    _fadeRect = new ColorRect
    {
      Color = new Color(0, 0, 0, 0),
      MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    AddChild(_fadeRect);
  }

  // 화면을 가린 채 다음 맵을 비동기로 로드하고, 지정된 PortalId 위치에 플레이어를 스폰한다.
  public async Task TravelToPortal(string mapScenePath, string spawnPortalId)
  {
    await FadeAsync(1f);

    PackedScene nextMap = await LoadSceneAsync(mapScenePath);
    if (nextMap != null)
    {
      MapManager.Instance?.ChangeMap(nextMap, spawnPortalId, spawnAtPortal: true);
    }

    await FadeAsync(0f);
  }

  private async Task FadeAsync(float targetAlpha)
  {
    Tween tween = CreateTween();
    tween.TweenProperty(_fadeRect, "color:a", targetAlpha, FadeDuration);
    await ToSignal(tween, Tween.SignalName.Finished);
  }

  private async Task<PackedScene> LoadSceneAsync(string path)
  {
    Error err = ResourceLoader.LoadThreadedRequest(path);
    if (err != Error.Ok)
    {
      GD.PrintErr($"[MapLoader] Failed to request load for '{path}': {err}");
      return null;
    }

    ResourceLoader.ThreadLoadStatus status;
    do
    {
      await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
      status = ResourceLoader.LoadThreadedGetStatus(path);
    } while (status == ResourceLoader.ThreadLoadStatus.InProgress);

    if (status != ResourceLoader.ThreadLoadStatus.Loaded)
    {
      GD.PrintErr($"[MapLoader] Failed to load '{path}': {status}");
      return null;
    }

    return ResourceLoader.LoadThreadedGet(path) as PackedScene;
  }
}
