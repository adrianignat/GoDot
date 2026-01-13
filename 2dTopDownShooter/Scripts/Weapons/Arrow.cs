using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Homing arrow projectile that targets the closest enemy at spawn time.
/// The target direction is calculated once in _Ready and maintained throughout flight.
/// Supports piercing (pass-through) ability.
/// </summary>
public partial class Arrow : RigidBody2D
{
	[Export]
	public ushort Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	/// <summary>
	/// Number of additional enemies this arrow can pierce through.
	/// Set by the Bow before the arrow is added to the scene.
	/// </summary>
	public int PiercingLevel { get; set; } = 0;

	/// <summary>
	/// Bonus damage percentage added to base damage.
	/// </summary>
	public float BonusDamagePercent { get; set; } = 0f;

	/// <summary>
	/// Critical hit chance percentage (0-100).
	/// </summary>
	public float CritChance { get; set; } = 0f;

	/// <summary>
	/// Critical hit damage multiplier (e.g., 1.5 = 150% damage).
	/// </summary>
	public float CritDamageMultiplier { get; set; } = 1.5f;

	/// <summary>
	/// Chance to freeze (slow) enemies on hit (0-100).
	/// </summary>
	public float FreezeChance { get; set; } = 0f;

	/// <summary>
	/// Chance to burn enemies on hit (0-100).
	/// </summary>
	public float BurnChance { get; set; } = 0f;

	/// <summary>
	/// Optional specific enemy to target (for split arrows).
	/// If null, uses normal targeting logic.
	/// </summary>
	public Node2D TargetOverride { get; set; } = null;

	private static readonly RandomNumberGenerator _rng = new();
	private Player _player;
	private int _enemiesHit = 0;
	private int _maxEnemiesCanHit;
	private HashSet<Enemy> _hitEnemies = new();
	private Vector2 _currentDirection;
	private float _currentSpeed;

	private bool IsPiercing => PiercingLevel > 0;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 10;

		// Add to arrows group for cleanup on restart
		AddToGroup(GameConstants.ArrowsGroup);

		_player = Game.Instance.Player;
		_maxEnemiesCanHit = 1 + PiercingLevel;
		_currentSpeed = Speed;

		UpdateVelocity();
	}

	private void AdjustPosition()
	{
		if (_player.Facing == Direction.W
			&& _player.Moving != Direction.S
			&& _player.Moving != Direction.SW)
			Position = new Vector2(GlobalPosition.X - 50, GlobalPosition.Y);
	}

	private void UpdateVelocity()
	{
		if (GameSettings.ShootingStyle == dTopDownShooter.Scripts.ShootingStyle.Directional)
		{
			// Directional mode: shoot in the direction the player is moving/facing
			_currentDirection = GetPlayerFacingDirection();
		}
		else
		{
			// Auto-aim mode: target closest enemy (or override target for split arrows)
			_currentDirection = new Vector2(-1, 0);

			Node2D target = TargetOverride ?? GetClosestEnemy();

			if (target != null && IsInstanceValid(target))
			{
				// Calculate the direction to the target
				Vector2 targetPosition = target.GlobalPosition;
				_currentDirection = (targetPosition - _player.GlobalPosition).Normalized();
			}
		}

		LinearVelocity = _currentDirection * _currentSpeed;
		Rotation = _currentDirection.Angle();
	}

	private Vector2 GetPlayerFacingDirection()
	{
		// Use Moving direction (based on input), fallback to Facing (E/W)
		return _player.Moving switch
		{
			Direction.N => new Vector2(0, -1),
			Direction.S => new Vector2(0, 1),
			Direction.E => new Vector2(1, 0),
			Direction.W => new Vector2(-1, 0),
			Direction.NE => new Vector2(1, -1).Normalized(),
			Direction.NW => new Vector2(-1, -1).Normalized(),
			Direction.SE => new Vector2(1, 1).Normalized(),
			Direction.SW => new Vector2(-1, 1).Normalized(),
			_ => _player.Facing == Direction.W ? new Vector2(-1, 0) : new Vector2(1, 0)
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		// Maintain speed, direction, and rotation regardless of physics collisions
		LinearVelocity = _currentDirection * _currentSpeed;
		AngularVelocity = 0;
		Rotation = _currentDirection.Angle();
	}

	private void OnCollision(Node body)
	{
		if (body is Enemy enemy)
		{
			// Skip if we already hit this enemy
			if (_hitEnemies.Contains(enemy))
				return;

			_hitEnemies.Add(enemy);

			// Calculate damage with bonus and crit
			float finalDamage = Damage * (1 + BonusDamagePercent / 100f);

			// Roll for crit
			if (CritChance > 0 && _rng.Randf() * 100f < CritChance)
			{
				finalDamage *= CritDamageMultiplier;
			}

			enemy.TakeDamage((ushort)finalDamage);

			// Roll for freeze effect
			if (FreezeChance > 0 && _rng.Randf() * 100f < FreezeChance)
			{
				enemy.ApplyFreeze(GameConstants.FreezeDuration, GameConstants.FreezeSlowPercent);
			}

			// Roll for burn effect
			if (BurnChance > 0 && _rng.Randf() * 100f < BurnChance)
			{
				enemy.ApplyBurn(GameConstants.BurnDuration, GameConstants.BurnDamagePerTick);
			}

			_enemiesHit++;

			// Add collision exception so arrow passes through this enemy
			AddCollisionExceptionWith(enemy);

			// Only destroy arrow if we've hit max enemies
			if (_enemiesHit >= _maxEnemiesCanHit)
			{
				QueueFree();
				return;
			}

		}
		else
		{
			// Hit something that's not an enemy (wall, etc.)
			QueueFree();
		}
	}

	private Node2D GetClosestEnemy()
	{
		Node2D closestEnemy = null;
		float closestDistance = Mathf.Inf;

		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);
		var viewRect = GetVisibleRect();

		foreach (Node2D enemy in enemies)
		{
			// Only target visible enemies
			if (!viewRect.HasPoint(enemy.GlobalPosition))
				continue;

			// Get the distance between the arrow and this enemy
			float distance = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);

			// Check if this enemy is closer than the previous closest enemy
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestEnemy = enemy;
			}
		}

		return closestEnemy;
	}

	private Rect2 GetVisibleRect()
	{
		var camera = _player.GetNode<Camera2D>("Camera2D");
		Vector2 center = camera.GetScreenCenterPosition();
		Vector2 halfSize = GetViewportRect().Size / camera.Zoom / 2;
		return new Rect2(center - halfSize, halfSize * 2);
	}
}
