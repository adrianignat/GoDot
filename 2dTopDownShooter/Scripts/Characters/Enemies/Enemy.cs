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
/// Uses NavigationAgent2D for pathfinding when stuck on obstacles.
/// </summary>
public abstract partial class Enemy : Character
{
	private const float ClickRadius = 50f; // How close a click needs to be to select this enemy

	// Stuck detection settings
	private const float StuckCheckInterval = 0.5f;    // How often to check if stuck
	private const float StuckDistanceThreshold = 10f; // Min distance to travel to not be stuck
	private const float UnstuckDistance = 100f;       // Distance to travel before switching back to direct
	private const float PathfindingCooldown = 3f;     // Seconds to wait before trying pathfinding again
	private const float NavTargetUpdateInterval = 0.5f; // How often to update navigation target (performance)
	private const float MaxNavigationTime = 3f;       // Max time in navigation mode before giving up

	// Debug: Static ID counter
	private static int _nextId = 1;

	// Debug: Instance properties
	public int EnemyId { get; private set; }
	public Vector2 SpawnPosition { get; private set; }
	private float _lastStuckCheckDistance;
	private bool _lastStuckCheckWasStuck;
	private int _stuckCount = 0;
	private int _pathfindingActivations = 0;

	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	protected Player _player;
	protected string _animationNodeName = "EnemyAnimations";

	// Navigation
	private NavigationAgent2D _navigationAgent;

	// Hybrid movement state
	private bool _usePathfinding = false;
	private Vector2 _lastStuckCheckPosition;
	private Vector2 _stuckPosition;
	private float _stuckCheckTimer = 0f;
	private float _pathfindingCooldownTimer = 0f;
	private float _navTargetUpdateTimer = 0f;      // Timer for periodic target updates (performance)
	private float _navigationTimer = 0f;           // Time spent in navigation mode
	private Vector2 _lastNavCheckPosition;         // For detecting stuck while navigating
	private int _navGiveUpCount = 0;               // Debug counter

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

		// Let subclass do additional initialization
		OnReady();

		// Initialize stuck detection
		_lastStuckCheckPosition = GlobalPosition;
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
	/// Override to prevent movement (e.g., while attacking).
	/// </summary>
	protected virtual bool CanMove() => true;

	/// <summary>
	/// Override to provide the direction the enemy wants to move.
	/// Default: chase directly toward player.
	/// </summary>
	protected virtual Vector2 GetIntendedDirection()
	{
		if (_player == null)
			return Vector2.Zero;
		return (_player.GlobalPosition - GlobalPosition).Normalized();
	}

	/// <summary>
	/// Override to customize animation updates based on movement direction.
	/// </summary>
	protected virtual void UpdateMovementAnimation(Vector2 direction)
	{
		if (direction.X != 0)
			_animation.FlipH = direction.X < 0;
	}

	/// <summary>
	/// Centralized movement handling with stuck detection.
	/// Subclasses should NOT override this - override GetIntendedDirection() and CanMove() instead.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove())
			return;

		Vector2 intendedDirection = GetIntendedDirection();
		Vector2 moveDirection = ApplyStuckDetection((float)delta, intendedDirection);

		Velocity = moveDirection * Speed;
		UpdateMovementAnimation(moveDirection);

		MoveAndSlide();
	}

	/// <summary>
	/// Applies stuck detection to any movement direction.
	/// If stuck, switches to NavigationAgent2D pathfinding to get around obstacles.
	/// </summary>
	protected Vector2 ApplyStuckDetection(float delta, Vector2 intendedDirection)
	{
		if (_player == null || _navigationAgent == null)
			return intendedDirection;

		// Update timers
		_stuckCheckTimer += delta;
		if (_pathfindingCooldownTimer > 0)
			_pathfindingCooldownTimer -= delta;

		if (_usePathfinding)
		{
			_navigationTimer += delta;

			// Check if we've traveled far enough to switch back
			float distanceFromStuckPoint = GlobalPosition.DistanceTo(_stuckPosition);

			if (distanceFromStuckPoint >= UnstuckDistance || _navigationAgent.IsNavigationFinished())
			{
				// We've made progress or navigation is done - switch back to normal movement
				ExitNavigationMode();
			}
			else if (_navigationTimer >= MaxNavigationTime)
			{
				// Check if we made any progress while navigating
				float navProgress = GlobalPosition.DistanceTo(_lastNavCheckPosition);
				if (navProgress < StuckDistanceThreshold)
				{
					// Navigation also failed - give up and try direct chase with cooldown
					_navGiveUpCount++;
					ExitNavigationMode();
					_pathfindingCooldownTimer = PathfindingCooldown;
				}
				else
				{
					// Made some progress, reset timer and keep navigating
					_navigationTimer = 0f;
					_lastNavCheckPosition = GlobalPosition;
				}
			}

			if (_usePathfinding)
			{
				// Continue following the navigation path
				return GetNavigationDirection(delta);
			}
		}

		// Normal movement mode - check if we're stuck
		if (_stuckCheckTimer >= StuckCheckInterval)
		{
			float distanceMoved = GlobalPosition.DistanceTo(_lastStuckCheckPosition);

			// Track debug info
			_lastStuckCheckDistance = distanceMoved;
			_lastStuckCheckWasStuck = distanceMoved < StuckDistanceThreshold;

			// Only try pathfinding if not on cooldown
			if (_lastStuckCheckWasStuck && intendedDirection != Vector2.Zero && _pathfindingCooldownTimer <= 0)
			{
				_stuckCount++;
				_pathfindingActivations++;

				// We're stuck - switch to pathfinding
				_usePathfinding = true;
				_stuckPosition = GlobalPosition;
				_navigationTimer = 0f;
				_lastNavCheckPosition = GlobalPosition;
				_navigationAgent.TargetPosition = _player.GlobalPosition;
			}

			// Reset for next check
			_lastStuckCheckPosition = GlobalPosition;
			_stuckCheckTimer = 0f;
		}

		return intendedDirection;
	}

	private void ExitNavigationMode()
	{
		_usePathfinding = false;
		_lastStuckCheckPosition = GlobalPosition;
		_stuckCheckTimer = 0f;
		_navigationTimer = 0f;
	}

	/// <summary>
	/// Gets the movement direction from NavigationAgent2D.
	/// Only updates target position periodically to improve performance.
	/// </summary>
	private Vector2 GetNavigationDirection(float delta)
	{
		if (_navigationAgent.IsNavigationFinished())
			return Vector2.Zero;

		// Update target position periodically (not every frame) for performance
		_navTargetUpdateTimer += delta;
		if (_navTargetUpdateTimer >= NavTargetUpdateInterval)
		{
			_navigationAgent.TargetPosition = _player.GlobalPosition;
			_navTargetUpdateTimer = 0f;
		}

		Vector2 nextPosition = _navigationAgent.GetNextPathPosition();
		return (nextPosition - GlobalPosition).Normalized();
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
		string movementMode = _usePathfinding ? "NAVIGATION" : "DIRECT_CHASE";
		string stuckResult = _lastStuckCheckWasStuck ? "STUCK" : "OK";

		float distanceToPlayer = _player != null
			? GlobalPosition.DistanceTo(_player.GlobalPosition)
			: -1f;

		float distanceFromStuck = _usePathfinding
			? GlobalPosition.DistanceTo(_stuckPosition)
			: 0f;

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
		string cooldownStatus = _pathfindingCooldownTimer > 0 ? $" (cooldown: {_pathfindingCooldownTimer:F1}s)" : "";
		GD.Print($"  Movement Mode:     {movementMode}{cooldownStatus}");
		if (_usePathfinding)
		{
			Vector2 nextPos = _navigationAgent.GetNextPathPosition();
			bool navFinished = _navigationAgent.IsNavigationFinished();
			GD.Print($"  Nav Finished:      {navFinished}");
			GD.Print($"  Nav Time:          {_navigationTimer:F1}s / {MaxNavigationTime}s");
			GD.Print($"  Next Nav Position: ({nextPos.X:F1}, {nextPos.Y:F1})");
			GD.Print($"  Stuck Position:    ({_stuckPosition.X:F1}, {_stuckPosition.Y:F1})");
			GD.Print($"  Progress from Stuck: {distanceFromStuck:F1}px / {UnstuckDistance}px");
		}
		GD.Print("───────────────────────────────────────────────────────────");
		GD.Print($"  Velocity:          ({Velocity.X:F1}, {Velocity.Y:F1})");
		GD.Print($"  Last Stuck Check:  {stuckResult}");
		GD.Print($"  Distance Moved:    {_lastStuckCheckDistance:F1}px (threshold: {StuckDistanceThreshold}px)");
		GD.Print($"  Total Stuck Count: {_stuckCount}");
		GD.Print($"  Navigation Uses:   {_pathfindingActivations}");
		GD.Print($"  Nav Give-ups:      {_navGiveUpCount}");
		GD.Print("═══════════════════════════════════════════════════════════");
	}

	#endregion

	internal override void OnKilled()
	{
		Game.Instance.EmitSignal(Game.SignalName.EnemyKilled, this);
		QueueFree();
	}

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
