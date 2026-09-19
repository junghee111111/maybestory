using Godot;

// 메이플스토리풍 대미지 인디케이터: 위로 떠오르며 페이드아웃, 크리티컬은 빨간색.
public partial class DamageIndicator : Node3D
{
  private static PackedScene _scene;

  [Export] public NodePath DamageLabelPath = "DamageLabel";
  [Export] public NodePath SubLabelPath = "SubLabel";
  [Export] public float FloatSpeed = 1.2f;
  [Export] public float Lifetime = 0.8f;

  private Label3D _damageLabel;
  private Label3D _subLabel;
  private float _elapsed;

  public static void Spawn(Node parent, Vector3 worldPosition, int damage, bool isCritical, string subText = "")
  {
    _scene ??= GD.Load<PackedScene>("res://ui/damageIndicator/DamageIndicator.tscn");

    var instance = _scene.Instantiate<DamageIndicator>();
    parent.AddChild(instance);
    instance.GlobalPosition = worldPosition;
    instance.Setup(damage, isCritical, subText);
  }

  public override void _Ready()
  {
    _damageLabel = GetNode<Label3D>(DamageLabelPath);
    _subLabel = GetNode<Label3D>(SubLabelPath);
  }

  public void Setup(int damage, bool isCritical, string subText)
  {
    _damageLabel.Text = damage.ToString();
    _damageLabel.Modulate = isCritical ? Colors.Red : Colors.White;

    _subLabel.Text = subText ?? string.Empty;
    _subLabel.Visible = !string.IsNullOrEmpty(subText);
  }

  public override void _Process(double delta)
  {
    float dt = (float)delta;
    _elapsed += dt;

    GlobalPosition += new Vector3(0, FloatSpeed * dt, 0);

    float alpha = Mathf.Clamp(1.0f - (_elapsed / Lifetime), 0f, 1f);
    _damageLabel.Modulate = new Color(_damageLabel.Modulate, alpha);
    _subLabel.Modulate = new Color(_subLabel.Modulate, alpha);

    if (_elapsed >= Lifetime)
    {
      QueueFree();
    }
  }
}
