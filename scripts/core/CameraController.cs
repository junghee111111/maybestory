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

		// clamp position
		float clampedX = Mathf.Clamp(targetPos.X, _minBounds.X, _maxBounds.X);
		float clampedY = Mathf.Clamp(targetPos.Y + RelativeYPos, _minBounds.Y, _maxBounds.Y);
		float desiredZ = targetPos.Z + _targetDistance;

		Vector3 desiredPos = new Vector3(clampedX, clampedY, desiredZ);
		GlobalPosition = GlobalPosition.Lerp(desiredPos, FollowSpeed * dt);
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
		float clampedX = Mathf.Clamp(_target.GlobalPosition.X, _minBounds.X, _maxBounds.X);
		float clampedY = Mathf.Clamp(_target.GlobalPosition.Y, _minBounds.Y, _maxBounds.Y);
		GlobalPosition = new Vector3(clampedX, clampedY, _target.GlobalPosition.Z + _targetDistance);
	}

}
