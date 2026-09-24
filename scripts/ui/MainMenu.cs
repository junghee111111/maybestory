using Godot;

public partial class MainMenu : Control
{
  [Export] public NodePath NewGameButtonPath = "CenterContainer/VBoxContainer/NewGameButton";
  [Export] public NodePath ContinueButtonPath = "CenterContainer/VBoxContainer/ContinueButton";
  [Export] public NodePath QuitButtonPath = "CenterContainer/VBoxContainer/QuitButton";

  public override void _Ready()
  {
    var newGameButton = GetNode<Button>(NewGameButtonPath);
    var continueButton = GetNode<Button>(ContinueButtonPath);
    var quitButton = GetNode<Button>(QuitButtonPath);

    continueButton.Disabled = SaveManager.Instance == null || !SaveManager.Instance.HasSaveFile();

    newGameButton.Pressed += OnNewGamePressed;
    continueButton.Pressed += OnContinuePressed;
    quitButton.Pressed += OnQuitPressed;
  }

  private void OnNewGamePressed()
  {
    GetTree().ChangeSceneToFile(SaveManager.GameWorldScenePath);
  }

  private void OnContinuePressed()
  {
    SaveManager.Instance?.Load();
    GetTree().ChangeSceneToFile(SaveManager.GameWorldScenePath);
  }

  private void OnQuitPressed()
  {
    GetTree().Quit();
  }
}
