using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Homing arrow projectile that targets the closest enemy at spawn time.
/// The target direction is calculated once in _Ready and maintained throughout flight.
/// Supports bouncing (ricochet) and piercing (pass-through) abilities.
/// </summary>
public partial class Arrow : RigidBody2D
{
	[Export]
	public ushort Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	[Export]
	public float BounceSpeedReduction = 0.3f;

	[Export]
	public float PierceSpeedReduction = 0.7f;

	/// <summary>
	/// Number of additional enemies this arrow can bounce through.
	/// Set by the Bow before the arrow is added to the scene.
	/// </summary>
	public int BouncingLevel { get; set; } = 0;

	/// <summary>
	/// Number of additional enemies this arrow can pierce through.
	/// Set by the Bow before the arrow is added to the scene.
	/// </summary>
	public int PiercingLevel { get; set; } = 0;

	private Player _player;
	private int _enemiesHit = 0;
	private int _maxEnemiesCanHit;
	private HashSet<Enemy> _hitEnemies = new();
	private Vector2 _currentDirection;
	private float _currentSpeed;

	private bool IsPiercing => PiercingLevel > 0;
	private bool IsBouncing => BouncingLevel > 0;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 10;

		// Add to arrows group for cleanup on restart
		AddToGroup(GameConstants.ArrowsGroup);

		_player = Game.Instance.Player;
		// Use whichever level is set (they're mutually exclusive)
		_maxEnemiesCanHit = 1 + BouncingLevel + PiercingLevel;
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
			// Auto-aim mode: target closest enemy
			_currentDirection = new Vector2(-1, 0);

			Node2D closestEnemy = GetClosestEnemy();

			if (closestEnemy != null)
			{
				// Calculate the direction to the enemy
				Vector2 enemyPosition = closestEnemy.GlobalPosition;
				_currentDirection = (enemyPosition - _player.GlobalPosition).Normalized();
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
			enemy.TakeDamage(Damage);
			_enemiesHit++;

			// Add collision exception so arrow passes through this enemy
			AddCollisionExceptionWith(enemy);

			// Only destroy arrow if we've hit max enemies
			if (_enemiesHit >= _maxEnemiesCanHit)
			{
				QueueFree();
				return;
			}

			if (IsPiercing)
			{
				// Piercing: maintain direction, slight speed reduction
				_currentSpeed *= PierceSpeedReduction;
			}
			else if (IsBouncing)
			{
				// Bouncing: reduce speed and change direction away from enemy
				_currentSpeed *= BounceSpeedReduction;

				// Calculate bounce direction (away from enemy + some randomness)
				Vector2 awayFromEnemy = (GlobalPosition - enemy.GlobalPosition).Normalized();
				float randomAngle = (float)GD.RandRange(-0.5f, 0.5f);
				_currentDirection = awayFromEnemy.Rotated(randomAngle);
				Rotation = _currentDirection.Angle();
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
