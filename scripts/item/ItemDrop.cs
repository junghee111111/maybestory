using Godot;
using System;

// 몬스터 처치 시 튀어나오는 드랍 아이템(또는 코인) 한 개의 물리 동작.
// 메이플스토리 스타일로 X,Y 초기 속도를 받아 포물선을 그리며 흩뿌려진 뒤 바닥에서 멈춘다.
public partial class ItemDrop : CharacterBody3D
{
    [Export] public NodePath VisualContainerPath = "VisualContainer";

    private const float Gravity = 20.0f;
    private const float GroundFriction = 20.0f;
    private const float SpinRadiansPerSecond = Mathf.Tau; // 1초에 360도 회전
    private const float PickupFlyDuration = 0.5f;
    private const float PickupLingerDuration = 0.5f;

    [Export] public Vector3 PickupTargetOffset = new Vector3(0, 2.2f, 0); // 플레이어 머리 쪽 위치 보정
    [Export] public float PickupArcHeight = 2.0f; // 날아가는 도중 머리 위로 솟는 높이

    private Node3D _visualContainer;
    private Node3D _collectTarget;
    private Action _onArrived;
    private Vector3 _collectStartPosition;
    private float _collectElapsed;
    private bool _isFlyingToTarget;

    public ItemData Item { get; private set; }
    public int CoinAmount { get; private set; }
    public bool IsBeingCollected { get; private set; }

    public override void _Ready()
    {
        _visualContainer = GetNode<Node3D>(VisualContainerPath);
        AddToGroup("ItemDrop");
    }

    public override void _Process(double delta)
    {
        _visualContainer.RotateY(SpinRadiansPerSecond * (float)delta);
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
        if (_isFlyingToTarget)
        {
            UpdateFlyToTarget((float)delta);
            return;
        }

        if (IsBeingCollected) return;

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

    // target을 매 물리 프레임 실시간으로 추적하며, 머리 위로 솟았다가 머리 쪽으로 떨어지는 뒤집힌 이차함수(포물선) 궤적으로 날아간 뒤 onArrived를 호출하고 0.5초 뒤 소멸한다.
    public void CollectTo(Node3D target, Action onArrived)
    {
        if (IsBeingCollected) return;
        IsBeingCollected = true;

        Velocity = Vector3.Zero;
        CollisionLayer = 0;
        CollisionMask = 0;

        _collectTarget = target;
        _onArrived = onArrived;
        _collectStartPosition = GlobalPosition;
        _collectElapsed = 0f;
        _isFlyingToTarget = true;
    }

    private void UpdateFlyToTarget(float dt)
    {
        _collectElapsed += dt;
        float t = Mathf.Clamp(_collectElapsed / PickupFlyDuration, 0f, 1f);
        float easedT = t * t; // easeIn

        Vector3 targetPos = _collectTarget != null ? _collectTarget.GlobalPosition + PickupTargetOffset : _collectStartPosition;

        Vector3 horizontal = _collectStartPosition.Lerp(targetPos, easedT);
        float baseY = Mathf.Lerp(_collectStartPosition.Y, targetPos.Y, easedT);
        float arc = PickupArcHeight * 4f * t * (1f - t); // -x^2 형태로 중간에 정점을 찍는 아치
        GlobalPosition = new Vector3(horizontal.X, baseY + arc, horizontal.Z);

        if (t >= 1f)
        {
            _isFlyingToTarget = false;
            _onArrived?.Invoke();
            _visualContainer.Visible = false;
            GetTree().CreateTimer(PickupLingerDuration).Timeout += QueueFree;
        }
    }
}
