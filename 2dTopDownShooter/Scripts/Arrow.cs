using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

public partial class Arrow : RigidBody2D
{
	[Export]
	public ushort Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	[Export]
	public float BounceSpeedReduction = 0.3f;

	/// <summary>
	/// Number of additional enemies this arrow can bounce through.
	/// Set by the Bow before the arrow is added to the scene.
	/// </summary>
	public int BouncingLevel { get; set; } = 0;

	private Player _player;
	private int _enemiesHit = 0;
	private int _maxEnemiesCanHit;
	private HashSet<Enemy> _hitEnemies = new();
	private Vector2 _currentDirection;
	private float _currentSpeed;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 10;

		_player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
		_maxEnemiesCanHit = 1 + BouncingLevel;
		_currentSpeed = Speed;

		UpdateVelocity();
		//AdjustPosition();
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
		_currentDirection = new Vector2(-1, 0);

		Node2D closestEnemy = GetClosestEnemy();

		if (closestEnemy != null)
		{
			// Calculate the direction to the enemy
			Vector2 enemyPosition = closestEnemy.GlobalPosition;
			_currentDirection = (enemyPosition - _player.GlobalPosition).Normalized();
		}

		LinearVelocity = _currentDirection * _currentSpeed;
		Rotation = _currentDirection.Angle();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Maintain speed and direction regardless of physics collisions
		LinearVelocity = _currentDirection * _currentSpeed;
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

			// Only destroy arrow if we've hit max enemies
			if (_enemiesHit >= _maxEnemiesCanHit)
			{
				QueueFree();
				return;
			}

			// Bounce: reduce speed and change direction away from enemy
			_currentSpeed *= BounceSpeedReduction;

			// Calculate bounce direction (away from enemy + some randomness)
			Vector2 awayFromEnemy = (GlobalPosition - enemy.GlobalPosition).Normalized();
			// Add slight random angle variation for more interesting bounces
			float randomAngle = (float)GD.RandRange(-0.5f, 0.5f);
			_currentDirection = awayFromEnemy.Rotated(randomAngle);
			Rotation = _currentDirection.Angle();
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

		// Assume all enemies are in a group called "Enemies"
		var enemies = GetTree().GetNodesInGroup("enemies");

		foreach (Node2D enemy in enemies)
		{
			// Get the distance between the player and this enemy
			float distance = _player.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);

			// Check if this enemy is closer than the previous closest enemy
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestEnemy = enemy;
			}
		}

		return closestEnemy;
	}
}
