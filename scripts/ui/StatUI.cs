using Godot;

// 메이플스토리 스타일 스탯창: MaxHp/MaxMp/STR/DEX/INT/LUK 표시 + AP 소모로 스탯 분배.
public partial class StatUI : UIWindowPanel
{
    [Export] public NodePath LevelLabelPath = "Panel/VBox/LevelLabel";
    [Export] public NodePath ApLabelPath = "Panel/VBox/ApLabel";
    [Export] public NodePath MaxHpLabelPath = "Panel/VBox/MaxHpLabel";
    [Export] public NodePath MaxMpLabelPath = "Panel/VBox/MaxMpLabel";
    [Export] public NodePath StrLabelPath = "Panel/VBox/StrRow/StrValueLabel";
    [Export] public NodePath DexLabelPath = "Panel/VBox/DexRow/DexValueLabel";
    [Export] public NodePath IntLabelPath = "Panel/VBox/IntRow/IntValueLabel";
    [Export] public NodePath LukLabelPath = "Panel/VBox/LukRow/LukValueLabel";
    [Export] public NodePath StrButtonPath = "Panel/VBox/StrRow/StrPlusButton";
    [Export] public NodePath DexButtonPath = "Panel/VBox/DexRow/DexPlusButton";
    [Export] public NodePath IntButtonPath = "Panel/VBox/IntRow/IntPlusButton";
    [Export] public NodePath LukButtonPath = "Panel/VBox/LukRow/LukPlusButton";

    private Label _levelLabel;
    private Label _apLabel;
    private Label _maxHpLabel;
    private Label _maxMpLabel;
    private Label _strLabel;
    private Label _dexLabel;
    private Label _intLabel;
    private Label _lukLabel;

    public override void _Ready()
    {
        WindowContentId = "UI_STAT";
        base._Ready();

        _levelLabel = GetNode<Label>(LevelLabelPath);
        _apLabel = GetNode<Label>(ApLabelPath);
        _maxHpLabel = GetNode<Label>(MaxHpLabelPath);
        _maxMpLabel = GetNode<Label>(MaxMpLabelPath);
        _strLabel = GetNode<Label>(StrLabelPath);
        _dexLabel = GetNode<Label>(DexLabelPath);
        _intLabel = GetNode<Label>(IntLabelPath);
        _lukLabel = GetNode<Label>(LukLabelPath);

        GetNode<Button>(StrButtonPath).Pressed += () => Allocate(StatKind.Str);
        GetNode<Button>(DexButtonPath).Pressed += () => Allocate(StatKind.Dex);
        GetNode<Button>(IntButtonPath).Pressed += () => Allocate(StatKind.Int);
        GetNode<Button>(LukButtonPath).Pressed += () => Allocate(StatKind.Luk);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += Refresh;
            PlayerStats.Instance.OnLevelUp += OnLevelUp;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= Refresh;
            PlayerStats.Instance.OnLevelUp -= OnLevelUp;
        }
    }

    protected override void OnOpened() => Refresh();

    private void OnLevelUp() => Refresh();

    private void Allocate(StatKind kind)
    {
        PlayerStats.Instance?.AllocateStatPoint(kind);
    }

    private void Refresh()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;

        _levelLabel.Text = $"Lv. {stats.Level}";
        _apLabel.Text = $"AP: {stats.StatPoints}";
        _maxHpLabel.Text = $"MaxHP: {stats.MaxHp}";
        _maxMpLabel.Text = $"MaxMP: {stats.MaxMp}";
        _strLabel.Text = $"STR: {stats.Str}";
        _dexLabel.Text = $"DEX: {stats.Dex}";
        _intLabel.Text = $"INT: {stats.Int}";
        _lukLabel.Text = $"LUK: {stats.Luk}";
    }
}
