using dTopDownShooter.Scripts;
using Godot;

public partial class Arrow : RigidBody2D
{
	[Export]
	public ushort Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	private Player _player;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 1;

		_player = GetTree().Root.GetNode("main").GetNode<Player>("Player");

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
		Vector2 direction = new Vector2(-1, 0);

		Node2D closestEnemy = GetClosestEnemy();

		if (closestEnemy != null)
		{
			// Calculate the direction to the enemy
			Vector2 enemyPosition = closestEnemy.GlobalPosition;
			direction = (enemyPosition - _player.GlobalPosition).Normalized();  // Normalized to get a unit vector
		}
			
		LinearVelocity = direction * Speed;
		Rotation = direction.Angle();
	}

	private void OnCollision(Node body)
	{
		if (body is Enemy)
		{
			var enemy = body as Enemy;
			enemy.TakeDamage(Damage);
		}
		QueueFree();
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
