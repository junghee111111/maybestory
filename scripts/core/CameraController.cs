using Godot;
using System;

public partial class CameraController : Node3D
{
	[Export] public float FollowSpeed = 3.5f;
	[Export] public float RelativeYPos = 10.0f;

	private Node3D _target;
	private Camera3D _camera;
	private Vector3 _minBounds;
	private Vector3 _maxBounds;
	private float _targetDistance = 12.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (_target == null) return;
		float dt = (float)delta;
		Vector3 targetPos = _target.GlobalPosition;

		// clamp position so the camera's own left/bottom/right/top edges stop at the map bounds, not its center.
		(float minX, float maxX, float minY, float maxY) = GetClampedCenterRange();
		float clampedX = Mathf.Clamp(targetPos.X, minX, maxX);
		float clampedY = Mathf.Clamp(targetPos.Y + RelativeYPos, minY, maxY);
		float desiredZ = targetPos.Z + _targetDistance;

		Vector3 desiredPos = new Vector3(clampedX, clampedY, desiredZ);
		GlobalPosition = GlobalPosition.Lerp(desiredPos, FollowSpeed * dt);
	}

	// 카메라가 보는 프레임의 절반 너비/높이(월드 단위)를 FOV와 거리로부터 계산한다.
	private Vector2 GetVisibleHalfExtents(float distance)
	{
		if (_camera == null) return Vector2.Zero;

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		float aspect = viewportSize.Y > 0 ? viewportSize.X / viewportSize.Y : 1.0f;
		float fovRadians = Mathf.DegToRad(_camera.Fov);

		float halfHeight, halfWidth;
		if (_camera.KeepAspect == Camera3D.KeepAspectEnum.Width)
		{
			halfWidth = distance * Mathf.Tan(fovRadians * 0.5f);
			halfHeight = halfWidth / aspect;
		}
		else
		{
			halfHeight = distance * Mathf.Tan(fovRadians * 0.5f);
			halfWidth = halfHeight * aspect;
		}

		return new Vector2(halfWidth, halfHeight);
	}

	// 카메라 중심(=이 노드의 위치)이 움직일 수 있는 범위. 프레임 가장자리가 맵 경계와 맞닿는 지점에서 멈춘다.
	private (float minX, float maxX, float minY, float maxY) GetClampedCenterRange()
	{
		Vector2 halfExtents = GetVisibleHalfExtents(_targetDistance);

		float minX = _minBounds.X + halfExtents.X;
		float maxX = _maxBounds.X - halfExtents.X;
		if (minX > maxX)
		{
			minX = maxX = (minX + maxX) * 0.5f;
		}

		float minY = _minBounds.Y + halfExtents.Y;
		float maxY = _maxBounds.Y - halfExtents.Y;
		if (minY > maxY)
		{
			minY = maxY = (minY + maxY) * 0.5f;
		}

		return (minX, maxX, minY, maxY);
	}

	public void SetBounds(Vector3 minBound, Vector3 maxBound, float cameraDistance)
	{
		_minBounds = minBound;
		_maxBounds = maxBound;
		_targetDistance = cameraDistance;

		if (_camera != null)
		{
			Vector3 camPos = _camera.Position;
			camPos.Z = _targetDistance;
			_camera.Position = camPos;
		}
		else
		{
			GD.PrintErr("Camera node not found. Cannot set camera distance.");
		}
	}

	public void SetTarget(Node3D target) => _target = target;

	public void SnapToTarget()
	{
		if (_target == null) return;
		(float minX, float maxX, float minY, float maxY) = GetClampedCenterRange();
		float clampedX = Mathf.Clamp(_target.GlobalPosition.X, minX, maxX);
		float clampedY = Mathf.Clamp(_target.GlobalPosition.Y, minY, maxY);
		GlobalPosition = new Vector3(clampedX, clampedY, _target.GlobalPosition.Z + _targetDistance);
	}

}
