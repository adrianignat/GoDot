using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using dTopDownShooter.Scripts.Upgrades;
using Godot;


public partial class Player : Character
{
	private ushort _gold = 0;
	private ColorRect _feetIndicator;
	private CanvasLayer _feetIndicatorLayer;
	private Camera2D _camera;

	public bool IsShooting = false;
	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }
	public int LuckLevel { get; private set; } = 0;

	// Dash state
	private bool _isDashing = false;
	private float _dashTimer = 0f;
	private float _dashCooldownTimer = 0f;
	private Vector2 _dashDirection;
	private uint _originalCollisionMask;

	// Dash constants - reference GameConstants for values
	private float DashDistance => GameConstants.DashDistance;
	private float DashDuration => GameConstants.DashDuration;
	private float DashCooldown => GameConstants.DashCooldown;

	public bool IsDashing => _isDashing;
	public bool CanDash => _dashCooldownTimer <= 0f && !_isDashing;
	public float DashCooldownRemaining => _dashCooldownTimer;

	// Reference GameConstants for external access
	public static float MaxMagnetRadius => GameConstants.MaxMagnetRadius;
	public static float MaxDynamiteBlastRadius => GameConstants.MaxDynamiteBlastRadius;

	public override void _Ready()
	{
		_animation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		_animation.Play("idle");
		_animation.AnimationFinished += OnAnimationFinished;
		Facing = Direction.E;
		Moving = Direction.E;

		_feetIndicatorLayer = GetNode<CanvasLayer>("FeetIndicatorLayer");
		_feetIndicator = _feetIndicatorLayer.GetNode<ColorRect>("FeetIndicator");
		_camera = GetNodeOrNull<Camera2D>("Camera2D");

		Game.Instance.PlayerTakeDamage += TakeDamage;
		Game.Instance.GoldAcquired += AcquireGold;
		Game.Instance.UpgradeSelected += UpgradeSelected;
		Game.Instance.NightDamageTick += OnNightDamageTick;
	}

	public override void _Notification(int what)
	{
		// Hide feet indicator when paused (menus showing)
		if (what == NotificationPaused && _feetIndicatorLayer != null)
		{
			_feetIndicatorLayer.Visible = false;
		}
		else if (what == NotificationUnpaused && _feetIndicatorLayer != null)
		{
			_feetIndicatorLayer.Visible = true;
		}
	}

	private void OnNightDamageTick()
	{
		// Take damage from the night
		TakeDamage(GameConstants.PlayerNightDamage);
		GD.Print($"Night damage! Health: {Health}");
	}

	internal override void TakeDamage(ushort damage)
	{
		// Ignore damage while dashing (invulnerable) or already dead
		if (_isDashing || IsDead) return;

		base.TakeDamage(damage);
	}

	private void UpgradeSelected(Upgrade upgdade)
	{
		var bow = GetNode<Bow>("Bow");

		if (upgdade.Type == UpgradeType.Speed)
			Speed += upgdade.Amount;
		else if (upgdade.Type == UpgradeType.Health)
			Health += upgdade.Amount;
		else if (upgdade.Type == UpgradeType.WeaponSpeed)
			bow.ObjectsPerSecond += (upgdade.Amount / 100f);
		else if (upgdade.Type == UpgradeType.Bouncing)
		{
			bow.BouncingLevel += 1;
			bow.SelectedArrowUpgrade = UpgradeType.Bouncing;
		}
		else if (upgdade.Type == UpgradeType.Piercing)
		{
			bow.PiercingLevel += 1;
			bow.SelectedArrowUpgrade = UpgradeType.Piercing;
		}
		else if (upgdade.Type == UpgradeType.Luck)
		{
			LuckLevel += upgdade.Amount;
		}
		else if (upgdade.Type == UpgradeType.Magnet)
		{
			var circle = GetMagnetShape();
			circle.Radius += upgdade.Amount * 5;
		}
		else if (upgdade.Type == UpgradeType.Dynamite)
		{
			var thrower = GetNode<DynamiteThrower>("DynamiteThrower");
			if (thrower.ObjectsPerSecond == 0)
			{
				// First upgrade: enable dynamite throwing
				thrower.ObjectsPerSecond = GameConstants.InitialDynamiteThrowRate;
			}
			// Increase blast radius with each upgrade
			thrower.BonusBlastRadius += upgdade.Amount;
		}
	}

	public override void _Process(double delta)
	{
		// Don't process input while dead
		if (IsDead) return;

		var move_input = Input.GetVector("left", "right", "up", "down");
		if (move_input != Vector2.Zero)
		{
			Facing = DirectionHelper.GetFacingDirection(move_input);
			Moving = DirectionHelper.GetMovingDirection(move_input);
		}
		IsShooting = _animation.Animation.ToString().Contains("shoot");
		if (IsShooting)
		{
			return;
		}

		if (move_input != Vector2.Zero)
		{
			_animation.Play("walk");
		}
		else
		{
			_animation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateFeetIndicator();

		// Don't process movement while dead
		if (IsDead) return;

		// Update dash cooldown
		if (_dashCooldownTimer > 0)
		{
			_dashCooldownTimer -= (float)delta;
		}

		// Check for dash input
		if (Input.IsActionJustPressed("dash") && CanDash)
		{
			StartDash();
		}

		// Process dash if active
		if (_isDashing)
		{
			ProcessDash(delta);
			return; // Skip normal movement while dashing
		}

		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * Speed;
		_animation.FlipH = Facing == Direction.W;

		MoveAndSlide();
	}

	private void UpdateFeetIndicator()
	{
		if (_feetIndicatorLayer == null || _feetIndicator == null) return;

		// Try to get camera if not cached yet
		if (_camera == null)
			_camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (_camera == null) return;

		// Player feet position in world space (offset down from center)
		Vector2 feetWorldPos = GlobalPosition + new Vector2(0, 20);

		// Convert to screen position using canvas transform
		var canvasTransform = GetCanvasTransform();
		Vector2 screenPos = canvasTransform * feetWorldPos;

		// Center the indicator on the feet position
		_feetIndicator.Position = screenPos - _feetIndicator.Size / 2;
	}

	public void AcquireGold(ushort amount)
	{
		_gold += amount;
	}

	internal void PlayShootAnimation()
	{
		if (IsShooting || IsDead)
			return;

		switch (Moving)
		{
			case Direction.N:
				_animation.Play("shoot_up");
				break;
			case Direction.S:
				_animation.Play("shoot_down");
				break;
			case Direction.SE:
			case Direction.SW:
				_animation.Play("shoot_down_forward");
				break;
			case Direction.NE:
			case Direction.NW:
				_animation.Play("shoot_up_forward");
				break;
			default:
				_animation.Play("shoot_forward");
				break;
		}
	}

	private void OnAnimationFinished()
	{
		// Check death first - IsShooting may be stale since _Process stops updating it
		if (IsDead)
		{
			// Death animation finished - now pause the game
			Game.Instance.IsPaused = true;
		}
		else if (IsShooting)
		{
			_animation.Play("idle");
		}
	}

	internal override void OnKilled()
	{
		// Clear shooting state and play death animation
		IsShooting = false;
		_animation.Play("dead");
	}

	public CircleShape2D GetMagnetShape()
	{
		var magnetShape = GetNode<Area2D>("GoldMagnetArea").GetNode<CollisionShape2D>("MagnetShape");
		return (CircleShape2D)magnetShape.Shape;
	}

	public DynamiteThrower GetDynamiteThrower()
	{
		return GetNode<DynamiteThrower>("DynamiteThrower");
	}

	private Vector2 DirectionToVector(Direction dir)
	{
		return dir switch
		{
			Direction.N => new Vector2(0, -1),
			Direction.S => new Vector2(0, 1),
			Direction.E => new Vector2(1, 0),
			Direction.W => new Vector2(-1, 0),
			Direction.NE => new Vector2(1, -1).Normalized(),
			Direction.NW => new Vector2(-1, -1).Normalized(),
			Direction.SE => new Vector2(1, 1).Normalized(),
			Direction.SW => new Vector2(-1, 1).Normalized(),
			_ => new Vector2(1, 0)
		};
	}

	private void StartDash()
	{
		if (!CanDash || IsDead) return;

		_dashDirection = DirectionToVector(Moving);

		// Calculate actual dash distance (may be reduced if water is in the way)
		float actualDashDistance = CalculateSafeDashDistance();

		if (actualDashDistance <= 0)
			return; // Can't dash at all (water right in front)

		_isDashing = true;
		_dashTimer = DashDuration * (actualDashDistance / DashDistance);
		_dashCooldownTimer = DashCooldown;

		// Store original collision mask and disable collisions during dash
		_originalCollisionMask = CollisionMask;
		CollisionMask = 0; // Disable all collisions during dash
	}

	private float CalculateSafeDashDistance()
	{
		Vector2 landingPos = GlobalPosition + _dashDirection * DashDistance;

		// If landing position is not in no-spawn zone, full dash
		if (!Game.Instance.IsInNoSpawnZone(landingPos))
			return DashDistance;

		// Landing is in no-spawn zone (water) - trace back to find safe spot
		const float stepSize = 10f;
		float distance = DashDistance;

		while (distance > 0)
		{
			distance -= stepSize;
			Vector2 checkPos = GlobalPosition + _dashDirection * distance;

			if (!Game.Instance.IsInNoSpawnZone(checkPos))
				return distance;
		}

		// No safe landing found
		return 0;
	}


	private void EndDash()
	{
		_isDashing = false;

		// Restore original collision mask
		CollisionMask = _originalCollisionMask;

		// Check if we ended inside an obstacle and push out if needed
		AdjustPositionIfInsideObstacle();
	}

	private void AdjustPositionIfInsideObstacle()
	{
		// Use a shape query to check if we're inside any obstacle
		var spaceState = GetWorld2D().DirectSpaceState;
		var collisionNode = GetNodeOrNull<CollisionShape2D>("PlayerCollisionShape");
		if (collisionNode == null || collisionNode.Shape == null) return;

		var shape = collisionNode.Shape;

		var query = new PhysicsShapeQueryParameters2D();
		query.Shape = shape;
		// Account for the collision shape's local position and player's scale
		Vector2 shapeGlobalPos = GlobalPosition + collisionNode.Position * Scale;
		// Create transform with scale built in
		query.Transform = new Transform2D(Scale.X, 0, 0, Scale.Y, shapeGlobalPos.X, shapeGlobalPos.Y);
		// Check against Map (layer 1) and Environment (layer 4) - water is handled during dash
		query.CollisionMask = GameConstants.MapCollisionLayer | (1 << 3);

		var results = spaceState.IntersectShape(query);

		if (results.Count > 0)
		{
			// We're inside an obstacle - move back along dash direction until we're clear
			Vector2 pushBackDirection = -_dashDirection;
			float pushBackStep = 5f;
			int maxIterations = 50; // Prevent infinite loop

			for (int i = 0; i < maxIterations; i++)
			{
				GlobalPosition += pushBackDirection * pushBackStep;
				shapeGlobalPos = GlobalPosition + collisionNode.Position * Scale;
				query.Transform = new Transform2D(Scale.X, 0, 0, Scale.Y, shapeGlobalPos.X, shapeGlobalPos.Y);
				results = spaceState.IntersectShape(query);

				if (results.Count == 0)
				{
					// We're clear, add a small extra buffer
					GlobalPosition += pushBackDirection * 5f;
					break;
				}
			}
		}
	}

	private void ProcessDash(double delta)
	{
		if (!_isDashing) return;

		float dashSpeed = DashDistance / DashDuration;
		Vector2 dashVelocity = _dashDirection * dashSpeed;

		Velocity = dashVelocity;
		MoveAndSlide(); // This will pass through everything since collision mask is 0

		_dashTimer -= (float)delta;
		if (_dashTimer <= 0)
		{
			EndDash();
		}
	}
}
