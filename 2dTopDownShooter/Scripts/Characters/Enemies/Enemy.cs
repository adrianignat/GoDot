using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;

public enum EnemyTier
{
	Blue = 1,   // 1 shot to kill (100 health)
	Red = 2,    // 2 shots to kill (200 health)
	Purple = 3, // 3 shots to kill (300 health)
	Yellow = 4  // 4 shots to kill (400 health)
}

/// <summary>
/// Base class for all enemy types. Handles common functionality like
/// tier-based health, sprite loading, movement toward player, and death.
/// Uses NavigationAgent2D for pathfinding.
/// </summary>
public abstract partial class Enemy : Character
{
	private const float ClickRadius = 50f; // How close a click needs to be to select this enemy
	private const float PathUpdateInterval = 0.25f; // Seconds between path recalculations (performance optimization)
	private const float StuckCheckInterval = 0.5f; // How often to check if stuck
	private const float StuckDistanceThreshold = 10f; // If moved less than this in StuckCheckInterval, consider stuck
	private const int StuckCountThreshold = 3; // How many stuck checks before entering unstuck mode
	private const float UnstuckDuration = 1.5f; // How long to move to random offset before resuming
	private const float UnstuckOffsetDistance = 150f; // How far to offset the unstuck target

	// Debug: Static ID counter
	private static int _nextId = 1;

	// Debug: Instance properties
	public int EnemyId { get; private set; }
	public Vector2 SpawnPosition { get; private set; }

	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	protected Player _player;
	protected string _animationNodeName = "EnemyAnimations";

	// Navigation
	private NavigationAgent2D _navigationAgent;
	private double _pathUpdateTimer;
	private double _stuckCheckTimer;
	private Vector2 _lastStuckCheckPosition;
	private int _stuckCount;
	private bool _isUnstuckMode;
	private double _unstuckTimer;
	private Vector2 _unstuckTarget;

	// Status effects
	private float _freezeTimer = 0f;
	private float _freezeSlowPercent = 0f;
	private float _originalSpeed;
	private bool _isFrozen = false;

	private float _burnTimer = 0f;
	private float _burnTickTimer = 0f;
	private ushort _burnDamagePerTick = 0;

	public override void _Ready()
	{
		// Assign unique ID and record spawn position
		EnemyId = _nextId++;
		SpawnPosition = GlobalPosition;

		_player = Game.Instance.Player;

		// Let subclass configure settings before we use them
		ConfigureEnemy();

		_animation = GetNode<AnimatedSprite2D>(_animationNodeName);

		// Get NavigationAgent2D
		_navigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");

		// Connect to avoidance callback for safe velocity computation
		_navigationAgent.VelocityComputed += OnVelocityComputed;

		// Randomize initial path update timer so all enemies don't recalculate on the same frame
		_pathUpdateTimer = GD.RandRange(0.0, PathUpdateInterval);
		_stuckCheckTimer = StuckCheckInterval;
		_lastStuckCheckPosition = GlobalPosition;

		// Add to enemies group for cleanup on day transition
		AddToGroup(GameConstants.EnemiesGroup);

		// Initialize health based on tier
		Health = (ushort)(GameConstants.EnemyBaseHealth * (int)Tier);

		// Let subclass load its specific sprites
		LoadSpriteForTier();

		// Connect animation finished for subclass handling
		_animation.AnimationFinished += OnAnimationFinished;

		// Start with walk animation
		_animation.Play("walk");

		// Store original speed for freeze effect
		_originalSpeed = Speed;

		// Let subclass do additional initialization
		OnReady();
	}

	/// <summary>
	/// Called before _Ready uses any settings. Override to set _animationNodeName or other config.
	/// </summary>
	protected virtual void ConfigureEnemy() { }

	/// <summary>
	/// Called after base _Ready completes. Override for subclass-specific initialization.
	/// </summary>
	protected virtual void OnReady() { }

	/// <summary>
	/// Load the appropriate sprite for the current tier.
	/// </summary>
	protected abstract void LoadSpriteForTier();

	/// <summary>
	/// Handle animation finished events. Override in subclass for specific behavior.
	/// </summary>
	protected virtual void OnAnimationFinished() { }

	/// <summary>
	/// Called when a body enters the attack range area. Override in subclass to handle melee attacks.
	/// </summary>
	public virtual void OnAttackRangeEntered(Node2D body) { }

	/// <summary>
	/// Called when a body exits the attack range area. Override in subclass to handle melee attacks.
	/// </summary>
	public virtual void OnAttackRangeExit(Node2D body) { }

	/// <summary>
	/// Override to prevent movement (e.g., while attacking or when in attack range).
	/// </summary>
	protected virtual bool CanMove() => true;

	/// <summary>
	/// Override to customize animation updates based on movement direction.
	/// </summary>
	protected virtual void UpdateMovementAnimation(Vector2 direction)
	{
		if (direction.X != 0)
			_animation.FlipH = direction.X < 0;
	}

	/// <summary>
	/// Override to customize the navigation target. Default is player position.
	/// Useful for enemies that need to maintain distance or retreat.
	/// </summary>
	protected virtual Vector2 GetNavigationTarget() => _player?.GlobalPosition ?? GlobalPosition;

	/// <summary>
	/// Gets the current navigation target, accounting for unstuck mode.
	/// </summary>
	private Vector2 GetCurrentNavigationTarget()
	{
		if (_isUnstuckMode)
			return _unstuckTarget;
		return GetNavigationTarget();
	}

	/// <summary>
	/// Enters unstuck mode with a random offset target to break oscillation patterns.
	/// </summary>
	private void EnterUnstuckMode()
	{
		_isUnstuckMode = true;
		_unstuckTimer = UnstuckDuration;
		_stuckCount = 0;

		// Pick a random direction and offset from current position
		float randomAngle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * UnstuckOffsetDistance;
		_unstuckTarget = GlobalPosition + offset;

		// Force immediate path recalculation to new target
		_pathUpdateTimer = 0;
	}

	/// <summary>
	/// Movement using NavigationAgent2D.
	/// Subclasses control movement via CanMove() override.
	/// Path recalculation is throttled for performance (PathUpdateInterval).
	/// Includes stuck detection to force path recalculation when not making progress.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		// Always update status effects (burn can kill even when not moving)
		UpdateStatusEffects(delta);

		if (!CanMove() || _player == null || _navigationAgent == null)
			return;

		// Handle unstuck mode timer
		if (_isUnstuckMode)
		{
			_unstuckTimer -= delta;
			if (_unstuckTimer <= 0)
			{
				_isUnstuckMode = false;
				_pathUpdateTimer = 0; // Force recalculation back to player
			}
		}

		// Stuck detection - track consecutive stuck checks
		_stuckCheckTimer -= delta;
		if (_stuckCheckTimer <= 0)
		{
			_stuckCheckTimer = StuckCheckInterval;
			float distanceMoved = GlobalPosition.DistanceTo(_lastStuckCheckPosition);

			if (distanceMoved < StuckDistanceThreshold)
			{
				_stuckCount++;

				if (_stuckCount >= StuckCountThreshold && !_isUnstuckMode)
				{
					// Enemy is dancing/oscillating - enter unstuck mode
					EnterUnstuckMode();
				}
				else
				{
					// Just stuck briefly - force path recalculation
					_pathUpdateTimer = 0;
				}
			}
			else
			{
				// Making progress - reset stuck count
				_stuckCount = 0;
			}

			_lastStuckCheckPosition = GlobalPosition;
		}

		// Only recalculate path periodically (not every frame) for performance
		_pathUpdateTimer -= delta;
		if (_pathUpdateTimer <= 0)
		{
			_pathUpdateTimer = PathUpdateInterval;
			_navigationAgent.TargetPosition = GetCurrentNavigationTarget();
		}

		if (_navigationAgent.IsNavigationFinished())
			return;

		Vector2 nextPosition = _navigationAgent.GetNextPathPosition();
		Vector2 direction = (nextPosition - GlobalPosition).Normalized();
		Vector2 desiredVelocity = direction * Speed;

		// Send velocity to navigation agent for avoidance calculation
		if (_navigationAgent.AvoidanceEnabled)
		{
			_navigationAgent.Velocity = desiredVelocity;
			// Movement happens in OnVelocityComputed callback
		}
		else
		{
			Velocity = desiredVelocity;
			UpdateMovementAnimation(direction);
			MoveAndSlide();
		}
	}

	/// <summary>
	/// Callback from NavigationAgent2D when safe velocity is computed (avoidance system).
	/// </summary>
	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		if (!CanMove())
			return;

		Velocity = safeVelocity;

		if (safeVelocity.LengthSquared() > 0)
		{
			UpdateMovementAnimation(safeVelocity.Normalized());
		}

		MoveAndSlide();
	}

	#region Debug

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			// Convert mouse position to world coordinates
			Vector2 mouseWorldPos = GetGlobalMousePosition();
			float distance = GlobalPosition.DistanceTo(mouseWorldPos);

			if (distance <= ClickRadius)
			{
				PrintDebugInfo();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void PrintDebugInfo()
	{
		float distanceToPlayer = _player != null
			? GlobalPosition.DistanceTo(_player.GlobalPosition)
			: -1f;

		string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");

		GD.Print("═══════════════════════════════════════════════════════════");
		GD.Print($"  [{timestamp}] ENEMY DEBUG - ID: {EnemyId} ({GetType().Name})");
		GD.Print("═══════════════════════════════════════════════════════════");
		GD.Print($"  Tier:              {Tier}");
		GD.Print($"  Health:            {Health}");
		GD.Print("───────────────────────────────────────────────────────────");
		GD.Print($"  Spawn Position:    ({SpawnPosition.X:F1}, {SpawnPosition.Y:F1})");
		GD.Print($"  Current Position:  ({GlobalPosition.X:F1}, {GlobalPosition.Y:F1})");
		GD.Print($"  Distance to Player: {distanceToPlayer:F1}px");
		GD.Print("───────────────────────────────────────────────────────────");
		GD.Print($"  CanMove:           {CanMove()}");
		if (_navigationAgent != null)
		{
			Vector2 nextPos = _navigationAgent.GetNextPathPosition();
			bool navFinished = _navigationAgent.IsNavigationFinished();
			GD.Print($"  Nav Finished:      {navFinished}");
			GD.Print($"  Next Nav Position: ({nextPos.X:F1}, {nextPos.Y:F1})");
		}
		GD.Print("───────────────────────────────────────────────────────────");
		GD.Print($"  Velocity:          ({Velocity.X:F1}, {Velocity.Y:F1})");
		GD.Print("═══════════════════════════════════════════════════════════");
	}

	#endregion

	internal override void OnKilled()
	{
		Game.Instance.EmitSignal(Game.SignalName.EnemyKilled, this);
		QueueFree();
	}

	#region Status Effects

	/// <summary>
	/// Apply freeze effect - slows enemy for duration.
	/// </summary>
	public void ApplyFreeze(float duration, float slowPercent)
	{
		_freezeTimer = duration;
		_freezeSlowPercent = slowPercent;

		if (!_isFrozen)
		{
			_isFrozen = true;
			Speed = (ushort)(_originalSpeed * (1 - slowPercent / 100f));
		}
	}

	/// <summary>
	/// Apply burn effect - deals damage over time.
	/// </summary>
	public void ApplyBurn(float duration, ushort damagePerTick)
	{
		_burnTimer = duration;
		_burnDamagePerTick = damagePerTick;
		// Reset tick timer to apply damage immediately on first tick
		_burnTickTimer = 0f;
	}

	private void UpdateStatusEffects(double delta)
	{
		// Handle freeze
		if (_freezeTimer > 0)
		{
			_freezeTimer -= (float)delta;
			if (_freezeTimer <= 0)
			{
				_isFrozen = false;
				Speed = (ushort)_originalSpeed;
			}
		}

		// Handle burn
		if (_burnTimer > 0)
		{
			_burnTimer -= (float)delta;
			_burnTickTimer -= (float)delta;

			if (_burnTickTimer <= 0)
			{
				_burnTickTimer = GameConstants.BurnTickInterval;
				TakeDamage(_burnDamagePerTick);
			}
		}
	}

	#endregion

	/// <summary>
	/// Helper to build animation frames from a sprite sheet.
	/// </summary>
	protected static void BuildAnimationFrames(
		SpriteFrames spriteFrames,
		Texture2D texture,
		(string name, int row, int frameCount, bool loop, float speed)[] animations,
		int frameSize = 192)
	{
		foreach (var (name, row, frameCount, loop, speed) in animations)
		{
			spriteFrames.AddAnimation(name);
			spriteFrames.SetAnimationLoop(name, loop);
			spriteFrames.SetAnimationSpeed(name, speed);

			for (int i = 0; i < frameCount; i++)
			{
				var atlasTexture = new AtlasTexture();
				atlasTexture.Atlas = texture;
				atlasTexture.Region = new Rect2(i * frameSize, row * frameSize, frameSize, frameSize);
				spriteFrames.AddFrame(name, atlasTexture);
			}
		}
	}
}
