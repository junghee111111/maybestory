using Godot;
using System;

public partial class Player : Life<PlayerStats>
{
	[ExportGroup("Maybe :: Node Settings")]
	[Export] public NodePath AnimationPlayerPath = "BaseChar/AnimationPlayer";
	[Export] public NodePath VisualModelPath = "BaseChar";
	[Export] public NodePath MobHitboxDetectorPath = "MobHitboxDetector";
	[Export] public float PickupRadius = 2.5f;

	private AnimationPlayer _animationPlayer;
	private Node3D _visualModel;
	private Area3D _mobHitboxDetector;
	private BaseMap _currentMap;
	private EquipManager _equipManager;
	private SkillManager _skillManager;
	private bool _isAttacking = false;

	// KeyboardManager 핫키 이벤트로 쪼인 입력을 물리 프레임까지 보관해둔다.
	private bool _attackQueued = false;
	private bool _jumpQueued = false;

	private readonly string[] _attackAnimations = { "Stab1", "Swing1", "Swing2", "Swing3", "Spell1" };

	public Player()
	{
		IsAttackable = true;
		HitBusyDuration = 1.0f;
		KnockbackThresholdPercentage = 1f; // 접촉 피격은 대미지량과 무관하게 항상 넉백
	}

	public override void _Ready()
	{
		base._Ready();

		_animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
		_visualModel = GetNode<Node3D>(VisualModelPath);
		_mobHitboxDetector = GetNode<Area3D>(MobHitboxDetectorPath);

		_animationPlayer.AnimationFinished += OnAnimationFinished;
		_mobHitboxDetector.AreaEntered += OnMobHitboxEntered;

		// Re-resolve the owning map whenever the player is (re)parented into a new map's scene tree.
		TreeEntered += RefreshCurrentMap;
		RefreshCurrentMap();

		// Equip basic sword
		_equipManager = GetNode<EquipManager>("EquipManager");
		_skillManager = GetNode<SkillManager>("SkillManager");

		if (KeyboardManager.Instance != null)
		{
			KeyboardManager.Instance.OnSlotActivated += OnHotkeyActivated;
		}
	}

	private void OnHotkeyActivated(HotkeySlot slot, SlotBinding binding)
	{
		if (binding.ContentType != SlotContentType.Skill)
		{
			return;
		}

		if (IsBusy)
		{
			return;
		}

		switch (binding.ContentId)
		{
			case "SkillJump":
				_jumpQueued = true;
				break;
			default:
				if (!_isAttacking)
					_skillManager.CastSkill(binding.ContentId);
				break;
		}
	}

	private void OnMobHitboxEntered(Area3D area)
	{
		if (IsBusy) return; // 이미 넉백 중이면 재갱신하지 않는다.
		if (area.GetParent() is not Mob mob) return;

		TakeDamage(mob.AttackPower, mob.GlobalPosition);
	}

	public override void TakeDamage(int[] damages, Vector3 hitSourcePosition, bool[] criticals, string subText = "")
	{
		base.TakeDamage(damages, hitSourcePosition, criticals, subText);

		// Busy 애니메이션이 공격 애니메이션을 가로채면 animation_finished가 발생하지 않아
		// _isAttacking이 계속 true로 남으므로 여기서 직접 풀어준다.
		if (!_isAttacking)
		{
			PlayAnimationIfNotPlaying("Busy", 0.1f);
		}
	}

	// 포탈 이동 등으로 화면이 가려진 동안 접촉 피격을 막기 위해 호출한다.
	public void SetHurtboxMonitoring(bool enabled)
	{
		_mobHitboxDetector.Monitoring = enabled;
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

		// Attack can be triggered while jumping, but not while already attacking or busy (knockback).
		bool attackRequested = _attackQueued;
		_attackQueued = false;
		if (attackRequested && !_isAttacking && !IsBusy)
		{
			StartAttack();
		}

		if (!_isAttacking && !IsBusy)
		{
			// Handle Jump.
			bool jumpRequested = _jumpQueued;
			_jumpQueued = false;
			if (jumpRequested && IsOnFloor())
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
			// Attacking/busy locks out movement/jump input; smoothly bleed off horizontal speed instead of stopping instantly.
			velocity.X = Mathf.MoveToward(velocity.X, 0, DecelerationWhileAttacking * dt);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, DecelerationWhileAttacking * dt);
		}

		Velocity = velocity;
		MoveAndSlide();
		ClampPositionToMapBounds();
	}

	public void StartAttack()
	{
		GD.Print($"[{Name}] Attack started.");
		_isAttacking = true;
	}

	public void TryPickUp()
	{
		ItemDrop closest = FindClosestDrop();
		closest?.CollectTo(this, () => ApplyPickup(closest));
	}

	private ItemDrop FindClosestDrop()
	{
		ItemDrop closest = null;
		float closestDist = PickupRadius;

		foreach (Node node in GetTree().GetNodesInGroup("ItemDrop"))
		{
			if (node is not ItemDrop drop || drop.IsBeingCollected) continue;

			float dist = drop.GlobalPosition.DistanceTo(GlobalPosition);
			if (dist <= closestDist)
			{
				closest = drop;
				closestDist = dist;
			}
		}

		return closest;
	}

	// 아이템이면 인벤토리로, 코인이면 소지금으로 반영한다.
	private void ApplyPickup(ItemDrop drop)
	{
		if (drop.Item != null)
		{
			InventoryManager.Instance?.AddItem(drop.Item, 1);
		}
		else
		{
			Stats.AddMoney(drop.CoinAmount);
		}
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

	private void PlayAnimationIfNotPlaying(string animationName, float blend = -1f)
	{
		if (_animationPlayer.CurrentAnimation != animationName && _animationPlayer.HasAnimation(animationName))
		{
			_animationPlayer.Play(animationName, blend);
		}
	}

	private void OnAnimationFinished(StringName animName)
	{
		foreach (string attackAnim in _attackAnimations)
		{
			if (animName == attackAnim)
			{
				_isAttacking = false;
				if (IsBusy) PlayAnimationIfNotPlaying("Busy", 0.1f);
				break;
			}
		}
	}

	public override void OnDeath()
	{
		_isAttacking = false;
		SetPhysicsProcess(false);
		PlayAnimationIfNotPlaying("Die");
		Stats?.ApplyDeathExpPenalty();
	}
}
