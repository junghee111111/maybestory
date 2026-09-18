using Godot;
using System;

public partial class Player : Life<PlayerStats>
{
	[ExportGroup("Maybe :: Node Settings")]
	[Export] public NodePath AnimationPlayerPath = "BaseChar/AnimationPlayer";
	[Export] public NodePath VisualModelPath = "BaseChar";

	private AnimationPlayer _animationPlayer;
	private Node3D _visualModel;
	private BaseMap _currentMap;
	private EquipManager _equipManager;
	private bool _isAttacking = false;


	private readonly string[] _attackAnimations = { "Stab1", "Swing1", "Swing2" };

	public override void _Ready()
	{
		base._Ready();

		_animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
		_visualModel = GetNode<Node3D>(VisualModelPath);

		_animationPlayer.AnimationFinished += OnAnimationFinished;

		// Re-resolve the owning map whenever the player is (re)parented into a new map's scene tree.
		TreeEntered += RefreshCurrentMap;
		RefreshCurrentMap();

		// Equip basic sword
		_equipManager = GetNode<EquipManager>("EquipManager");
	}

	private void RefreshCurrentMap()
	{
		// root node => MapContainer => BaseMap
		_currentMap = GetTree().Root.GetNodeOrNull<BaseMap>("GameWorld/MapContainer/BaseMap");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector3 velocity = Velocity;

		// Add the gravity, using the current map's value when available.
		if (!IsOnFloor())
		{
			float gravity = _currentMap != null ? _currentMap.Gravity : GetGravity().Length();
			velocity.Y -= gravity * dt;
		}

		// Attack can be triggered while jumping, but not while already attacking.
		if (Input.IsActionJustPressed("attack") && !_isAttacking)
		{
			StartAttack();
		}

		if (!_isAttacking)
		{
			// Handle Jump.
			if (Input.IsActionJustPressed("jump") && IsOnFloor())
			{
				velocity.Y = JumpVelocity;
			}

			float moveInput = Input.GetAxis("move_left", "move_right");
			float targetSpeedX = moveInput * Speed;
			float rate = Mathf.Abs(moveInput) > 0.01f ? Acceleration : Deceleration;
			velocity.X = Mathf.MoveToward(velocity.X, targetSpeedX, rate * dt);

			if (moveInput > 0.01f)
			{
				_visualModel.RotationDegrees = new Vector3(0, 90, 0);
			}
			else if (moveInput < -0.01f)
			{
				_visualModel.RotationDegrees = new Vector3(0, -90, 0);
			}

			UpdateLocomotionAnimation(moveInput);
		}
		else
		{
			// Attacking locks out movement/jump input; smoothly bleed off horizontal speed instead of stopping instantly.
			velocity.X = Mathf.MoveToward(velocity.X, 0, DecelerationWhileAttacking * dt);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, DecelerationWhileAttacking * dt);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void StartAttack()
	{
		_isAttacking = true;
		string anim = _attackAnimations[GD.Randi() % (uint)_attackAnimations.Length];
		_animationPlayer.Play(anim);
	}

	private void UpdateLocomotionAnimation(float moveInput)
	{
		if (!IsOnFloor())
		{
			PlayAnimationIfNotPlaying("Jump");
		}
		else if (Mathf.Abs(moveInput) > 0.01f)
		{
			PlayAnimationIfNotPlaying("Walk");
		}
		else
		{
			PlayAnimationIfNotPlaying("Idle");
		}
	}

	private void PlayAnimationIfNotPlaying(string animationName)
	{
		if (_animationPlayer.CurrentAnimation != animationName && _animationPlayer.HasAnimation(animationName))
		{
			_animationPlayer.Play(animationName);
		}
	}

	private void OnAnimationFinished(StringName animName)
	{
		foreach (string attackAnim in _attackAnimations)
		{
			if (animName == attackAnim)
			{
				_isAttacking = false;
				break;
			}
		}
	}

	public override void OnDeath()
	{
		_isAttacking = false;
		SetPhysicsProcess(false);
		PlayAnimationIfNotPlaying("Die");
	}
}
