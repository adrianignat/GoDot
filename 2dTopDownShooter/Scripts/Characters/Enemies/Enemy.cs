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
	/// Movement using NavigationAgent2D.
	/// Subclasses control movement via CanMove() override.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove() || _player == null || _navigationAgent == null)
			return;

		_navigationAgent.TargetPosition = _player.GlobalPosition;

		if (_navigationAgent.IsNavigationFinished())
			return;

		Vector2 nextPosition = _navigationAgent.GetNextPathPosition();
		Vector2 direction = (nextPosition - GlobalPosition).Normalized();

		Velocity = direction * Speed;
		UpdateMovementAnimation(direction);

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
