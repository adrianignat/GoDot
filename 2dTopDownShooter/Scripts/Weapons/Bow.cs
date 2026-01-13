using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Spawners;
using Godot;
using System.Collections.Generic;

public partial class Bow : Spawner<Arrow>
{
	private float _timeUntilNextFire = 0f;

	private bool _arrowQueued = false;

	private AnimatedSprite2D _playerAnimation;
	private Player _player;
	private Camera2D _camera;

	// Animation speed scaling
	private float _initialFireRate;
	private float _baseAnimationDuration;

	/// <summary>
	/// Number of additional enemies arrows can pierce through.
	/// </summary>
	public int PiercingLevel { get; set; } = 0;

	/// <summary>
	/// Bonus damage percentage added to arrows.
	/// </summary>
	public float BonusDamagePercent { get; set; } = 0f;

	/// <summary>
	/// Critical hit chance percentage (0-100).
	/// </summary>
	public float CritChance { get; set; } = 5f;

	/// <summary>
	/// Critical hit damage multiplier (e.g., 1.5 = 150% damage).
	/// </summary>
	public float CritDamageMultiplier { get; set; } = 1.5f;

	/// <summary>
	/// Chance to freeze enemies on hit (0-100).
	/// </summary>
	public float FreezeChance { get; set; } = 0f;

	/// <summary>
	/// Chance to burn enemies on hit (0-100).
	/// </summary>
	public float BurnChance { get; set; } = 0f;

	/// <summary>
	/// Chance to split arrows (0-100 percent).
	/// </summary>
	public float ArrowSplitChance { get; set; } = 0f;

	private static readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
		_player = GetParent<Player>();
		_camera = _player.GetNode<Camera2D>("Camera2D");

		// Store initial fire rate to calculate animation speed scaling
		_initialFireRate = ObjectsPerSecond;

		// Calculate base animation duration (frames / fps)
		// shoot_forward has ~5 frames at 10fps = 0.5 seconds
		var spriteFrames = _playerAnimation.SpriteFrames;
		if (spriteFrames != null && spriteFrames.HasAnimation("shoot_forward"))
		{
			int frameCount = spriteFrames.GetFrameCount("shoot_forward");
			double fps = spriteFrames.GetAnimationSpeed("shoot_forward");
			_baseAnimationDuration = (float)(frameCount / fps);
		}
		else
		{
			_baseAnimationDuration = 0.5f; // fallback estimate
		}
	}

	/// <summary>
	/// Returns the animation speed scale needed to match the current fire rate.
	/// </summary>
	public float GetAnimationSpeedScale()
	{
		// Time between shots at current fire rate
		float targetFireInterval = 1f / ObjectsPerSecond;

		// If animation is already faster than fire rate, no scaling needed
		if (_baseAnimationDuration <= targetFireInterval)
			return 1f;

		// Scale animation to fit within fire interval
		float requiredScale = _baseAnimationDuration / targetFireInterval;

		// Clamp to max scale
		return Mathf.Min(requiredScale, GameConstants.MaxShootAnimationSpeedScale);
	}

	public override bool CanSpawn()
	{
		// In auto-aim mode, don't shoot if no enemies are visible
		// In directional mode, always allow shooting
		if (GameSettings.ShootingStyle == dTopDownShooter.Scripts.ShootingStyle.AutoAim && !HasVisibleEnemy())
		{
			_arrowQueued = false;
			return false;
		}

		if (!_player.IsShooting && !_arrowQueued)
		{
			_arrowQueued = true;

			// Apply animation speed scale to match fire rate
			_playerAnimation.SpeedScale = GetAnimationSpeedScale();

			_player.PlayShootAnimation();
			return false;
		}
		else if (!_player.IsShooting && _arrowQueued)
		{
			_arrowQueued = false;

			// Reset animation speed after shooting
			_playerAnimation.SpeedScale = 1f;

			return true;
		}
		return false;
	}

	private bool HasVisibleEnemy()
	{
		var viewRect = GetVisibleRect();
		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);

		foreach (Node2D enemy in enemies)
		{
			if (viewRect.HasPoint(enemy.GlobalPosition))
				return true;
		}
		return false;
	}

	public Rect2 GetVisibleRect()
	{
		Vector2 center = _camera.GetScreenCenterPosition();
		Vector2 halfSize = GetViewportRect().Size / _camera.Zoom / 2;
		return new Rect2(center - halfSize, halfSize * 2);
	}

	protected override void InitializeSpawnedObject(Arrow arrow)
	{
		arrow.PiercingLevel = PiercingLevel;
		arrow.BonusDamagePercent = BonusDamagePercent;
		arrow.CritChance = CritChance;
		arrow.CritDamageMultiplier = CritDamageMultiplier;
		arrow.FreezeChance = FreezeChance;
		arrow.BurnChance = BurnChance;
	}

	protected override void Spawn()
	{
		// Spawn the main arrow
		base.Spawn();

		// Roll for split arrow if enabled and in auto-aim mode
		if (ArrowSplitChance > 0 && GameSettings.ShootingStyle == dTopDownShooter.Scripts.ShootingStyle.AutoAim)
		{
			if (_rng.Randf() * 100f < ArrowSplitChance)
			{
				SpawnSplitArrow();
			}
		}
	}

	private void SpawnSplitArrow()
	{
		// Get visible enemies sorted by distance
		var enemies = GetVisibleEnemiesSortedByDistance();

		// Need at least 2 enemies (main arrow targets closest, split targets second)
		if (enemies.Count < 2)
			return;

		var targetEnemy = enemies[1]; // Target the second closest enemy

		var splitArrow = Scene.Instantiate<Arrow>();
		splitArrow.GlobalPosition = GetLocation();

		// Initialize with same properties as main arrow
		InitializeSpawnedObject(splitArrow);

		// Set target override for this split arrow
		splitArrow.TargetOverride = targetEnemy;

		GetSpawnParent().AddChild(splitArrow);
	}

	private List<Node2D> GetVisibleEnemiesSortedByDistance()
	{
		var viewRect = GetVisibleRect();
		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);
		var visibleEnemies = new List<(Node2D enemy, float distance)>();

		foreach (Node2D enemy in enemies)
		{
			if (viewRect.HasPoint(enemy.GlobalPosition))
			{
				float distance = _player.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
				visibleEnemies.Add((enemy, distance));
			}
		}

		// Sort by distance (closest first)
		visibleEnemies.Sort((a, b) => a.distance.CompareTo(b.distance));

		// Extract just the enemies
		var result = new List<Node2D>();
		foreach (var (enemy, _) in visibleEnemies)
		{
			result.Add(enemy);
		}

		return result;
	}
}
