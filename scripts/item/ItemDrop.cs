using Godot;

// 몬스터 처치 시 튀어나오는 드랍 아이템(또는 코인) 한 개의 물리 동작.
// 메이플스토리 스타일로 X,Y 초기 속도를 받아 포물선을 그리며 흩뿌려진 뒤 바닥에서 멈춘다.
public partial class ItemDrop : CharacterBody3D
{
    [Export] public NodePath VisualContainerPath = "VisualContainer";

    private const float Gravity = 20.0f;
    private const float GroundFriction = 20.0f;

    private Node3D _visualContainer;

    public ItemData Item { get; private set; }
    public int CoinAmount { get; private set; }

    public override void _Ready()
    {
        _visualContainer = GetNode<Node3D>(VisualContainerPath);
    }

    // item이 null이면 코인 드랍으로 취급한다.
    public void Setup(ItemData item, int coinAmount, PackedScene visualScene, Vector3 initialVelocity)
    {
        Item = item;
        CoinAmount = coinAmount;
        Velocity = initialVelocity;

        if (visualScene != null)
        {
            _visualContainer.AddChild(visualScene.Instantiate());
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 vel = Velocity;

        if (!IsOnFloor())
        {
            vel.Y -= Gravity * dt;
        }
        else
        {
            vel.X = Mathf.MoveToward(vel.X, 0, GroundFriction * dt);
        }

        vel.Z = 0.0f;
        Velocity = vel;
        MoveAndSlide();
    }
}
