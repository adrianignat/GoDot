using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;
using System.Collections.Generic;

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
/// </summary>
public abstract partial class Enemy : Character
{
	private const int TileSize = 64;
	private const float ClickRadius = 50f; // How close a click needs to be to select this enemy

	// Stuck detection settings
	private const float StuckCheckInterval = 0.5f;    // How often to check if stuck
	private const float StuckDistanceThreshold = 10f; // Min distance to travel to not be stuck
	private const float UnstuckDistance = 100f;       // Distance to travel before switching back to direct
	private const int MaxWaypointSkipsBeforeGiveUp = 5; // Give up pathfinding after this many skips
	private const float PathfindingCooldown = 3f;     // Seconds to wait before trying pathfinding again

	// Debug: Static ID counter
	private static int _nextId = 1;

	// Debug: Instance properties
	public int EnemyId { get; private set; }
	public Vector2 SpawnPosition { get; private set; }
	private float _lastStuckCheckDistance;
	private bool _lastStuckCheckWasStuck;
	private int _stuckCount = 0;
	private int _pathfindingActivations = 0;
	private int _waypointsSkipped = 0;

	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	protected Player _player;
	protected string _animationNodeName = "EnemyAnimations";

	// Pathfinding
	protected AStarGrid2D _astar;
	private Queue<Vector2> _path = new();

	// Hybrid movement state
	private bool _usePathfinding = false;
	private Vector2 _lastStuckCheckPosition;
	private Vector2 _stuckPosition;
	private float _stuckCheckTimer = 0f;
	private Vector2 _lastPathfindingCheckPosition;
	private float _pathfindingStuckTimer = 0f;
	private int _currentAttemptSkips = 0;  // Skips in current pathfinding attempt
	private float _pathfindingCooldownTimer = 0f;  // Cooldown before retrying pathfinding





	public override void _Ready()
	{
		// Assign unique ID and record spawn position
		EnemyId = _nextId++;
		SpawnPosition = GlobalPosition;

		_player = Game.Instance.Player;

		// Let subclass configure settings before we use them
		ConfigureEnemy();

		_animation = GetNode<AnimatedSprite2D>(_animationNodeName);

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



		
		// Get A* grid from MapGenerator
		var mapNode = GetTree().GetFirstNodeInGroup("map");
		if (mapNode is MapGenerator mapGenerator)
		{
			_astar = mapGenerator.GetAStar();
		}

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
	/// If stuck, switches to A* pathfinding to get around obstacles.
	/// Use this in _PhysicsProcess for any enemy type.
	/// </summary>
	/// <param name="delta">Delta time from _PhysicsProcess</param>
	/// <param name="intendedDirection">The direction the enemy wants to move (can be toward player, away, etc.)</param>
	/// <returns>Either the intended direction or a pathfinding direction if stuck</returns>
	protected Vector2 ApplyStuckDetection(float delta, Vector2 intendedDirection)
	{
		if (_player == null)
			return intendedDirection;

		// Update timers
		_stuckCheckTimer += delta;
		if (_pathfindingCooldownTimer > 0)
			_pathfindingCooldownTimer -= delta;

		if (_usePathfinding)
		{
			// Currently in pathfinding mode - check if we've traveled far enough to switch back
			float distanceFromStuckPoint = GlobalPosition.DistanceTo(_stuckPosition);

			if (distanceFromStuckPoint >= UnstuckDistance || _path.Count == 0)
			{
				// We've made progress or path is done - switch back to normal movement
				_usePathfinding = false;
				_path.Clear();
				_lastStuckCheckPosition = GlobalPosition;
				_stuckCheckTimer = 0f;
				_pathfindingStuckTimer = 0f;
			}
			else
			{
				// Check if we're stuck while following the path
				_pathfindingStuckTimer += delta;
				if (_pathfindingStuckTimer >= StuckCheckInterval)
				{
					float distanceMoved = GlobalPosition.DistanceTo(_lastPathfindingCheckPosition);
					if (distanceMoved < StuckDistanceThreshold && _path.Count > 0)
					{
						// Stuck while pathfinding - skip current waypoint
						_path.Dequeue();
						_waypointsSkipped++;
						_currentAttemptSkips++;

						// Check if we should give up on pathfinding
						if (_currentAttemptSkips >= MaxWaypointSkipsBeforeGiveUp)
						{
							// Give up - pathfinding isn't working, use direct movement
							_usePathfinding = false;
							_path.Clear();
							_pathfindingCooldownTimer = PathfindingCooldown;
							_lastStuckCheckPosition = GlobalPosition;
							_stuckCheckTimer = 0f;
							return intendedDirection;
						}

						if (_path.Count == 0)
						{
							// No more waypoints - recalculate
							RecalculatePath();
						}
					}
					_lastPathfindingCheckPosition = GlobalPosition;
					_pathfindingStuckTimer = 0f;
				}

				// Continue following the path
				return GetPathDirection();
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
				_lastPathfindingCheckPosition = GlobalPosition;
				_pathfindingStuckTimer = 0f;
				_currentAttemptSkips = 0;  // Reset skip counter for new attempt
				RecalculatePath();

				if (_path.Count > 0)
				{
					return GetPathDirection();
				}
				// If pathfinding failed, continue with intended direction
				_usePathfinding = false;
			}

			// Reset for next check
			_lastStuckCheckPosition = GlobalPosition;
			_stuckCheckTimer = 0f;
		}

		return intendedDirection;
	}

	/// <summary>
	/// Shorthand for chasing the player with stuck detection.
	/// </summary>
	protected Vector2 GetChaseDirection(float delta)
	{
		if (_player == null)
			return Vector2.Zero;

		Vector2 toPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
		return ApplyStuckDetection(delta, toPlayer);
	}

	private Vector2 GetPathDirection()
	{
		if (_path.Count == 0)
			return Vector2.Zero;

		Vector2 target = _path.Peek();

		// Check if we've reached the current waypoint
		if (GlobalPosition.DistanceTo(target) < TileSize / 2f)
		{
			_path.Dequeue();
			if (_path.Count == 0)
				return Vector2.Zero;
			target = _path.Peek();
		}

		return (target - GlobalPosition).Normalized();
	}

	private void RecalculatePath()
	{
		if (_astar == null || _player == null)
			return;

		Vector2I startCell = WorldToCell(GlobalPosition);
		Vector2I endCell = WorldToCell(_player.GlobalPosition);

		if (!_astar.IsInBoundsv(startCell) || !_astar.IsInBoundsv(endCell))
			return;

		if (_astar.IsPointSolid(startCell) || _astar.IsPointSolid(endCell))
			return;

		_path.Clear();

		var cellPath = _astar.GetIdPath(startCell, endCell);

		if (cellPath.Count == 0)
			return;

		foreach (Vector2I cell in cellPath)
		{
			Vector2 worldPos = CellToWorld(cell);
			_path.Enqueue(worldPos);
		}
	}

	private Vector2I WorldToCell(Vector2 worldPos)
	{
		return new Vector2I(
			Mathf.FloorToInt(worldPos.X / TileSize),
			Mathf.FloorToInt(worldPos.Y / TileSize)
		);
	}

	private Vector2 CellToWorld(Vector2I cell)
	{
		return new Vector2(
			cell.X * TileSize + TileSize / 2f,
			cell.Y * TileSize + TileSize / 2f
		);
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
		string movementMode = _usePathfinding ? "PATHFINDING" : "DIRECT_CHASE";
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
		GD.Print($"  Current Cell:      ({Mathf.FloorToInt(GlobalPosition.X / TileSize)}, {Mathf.FloorToInt(GlobalPosition.Y / TileSize)})");
		GD.Print($"  Distance to Player: {distanceToPlayer:F1}px");
		GD.Print("───────────────────────────────────────────────────────────");
		string cooldownStatus = _pathfindingCooldownTimer > 0 ? $" (cooldown: {_pathfindingCooldownTimer:F1}s)" : "";
		GD.Print($"  Movement Mode:     {movementMode}{cooldownStatus}");
		GD.Print($"  Path Waypoints:    {_path.Count}");
		if (_usePathfinding && _path.Count > 0)
		{
			Vector2 nextWaypoint = _path.Peek();
			Vector2 dirToWaypoint = (nextWaypoint - GlobalPosition).Normalized();
			float distToWaypoint = GlobalPosition.DistanceTo(nextWaypoint);
			GD.Print($"  Next Waypoint:     ({nextWaypoint.X:F1}, {nextWaypoint.Y:F1})");
			GD.Print($"  Waypoint Cell:     ({Mathf.FloorToInt(nextWaypoint.X / TileSize)}, {Mathf.FloorToInt(nextWaypoint.Y / TileSize)})");
			GD.Print($"  Dist to Waypoint:  {distToWaypoint:F1}px");
			GD.Print($"  Direction:         ({dirToWaypoint.X:F2}, {dirToWaypoint.Y:F2})");
			GD.Print($"  Stuck Position:    ({_stuckPosition.X:F1}, {_stuckPosition.Y:F1})");
			GD.Print($"  Progress from Stuck: {distanceFromStuck:F1}px / {UnstuckDistance}px");
		}
		else if (_usePathfinding)
		{
			GD.Print($"  Stuck Position:    ({_stuckPosition.X:F1}, {_stuckPosition.Y:F1})");
			GD.Print($"  Progress from Stuck: {distanceFromStuck:F1}px / {UnstuckDistance}px");
		}
		GD.Print("───────────────────────────────────────────────────────────");
		GD.Print($"  Velocity:          ({Velocity.X:F1}, {Velocity.Y:F1})");
		GD.Print($"  Last Stuck Check:  {stuckResult}");
		GD.Print($"  Distance Moved:    {_lastStuckCheckDistance:F1}px (threshold: {StuckDistanceThreshold}px)");
		GD.Print($"  Total Stuck Count: {_stuckCount}");
		GD.Print($"  Pathfinding Uses:  {_pathfindingActivations}");
		GD.Print($"  Waypoints Skipped: {_waypointsSkipped}");
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
