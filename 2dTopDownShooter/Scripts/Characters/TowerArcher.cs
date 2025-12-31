using dTopDownShooter.Scripts;
using Godot;

public partial class TowerArcher : Node2D
{
	[Export] public PackedScene ArrowScene;
	[Export] public float FireRate = 1.5f; // arrows per second
	[Export] public float DetectionRange = 400f;
	[Export] public float PlayerActivationRange = 500f;

	private AnimatedSprite2D _sprite;
	private Timer _fireTimer;
	private bool _isShooting = false;
	private Vector2? _pendingTargetPosition = null;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_fireTimer = GetNode<Timer>("FireTimer");

		_fireTimer.WaitTime = 1f / FireRate;
		_fireTimer.Timeout += OnFireTimerTimeout;

		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.Play("idle");

		if (ArrowScene == null)
			ArrowScene = GD.Load<PackedScene>("res://Entities/Projectiles/tower_arrow.tscn");
	}

	private bool IsPlayerInRange()
	{
		var player = Game.Instance?.Player;
		if (player == null)
			return false;

		// Use parent (tower) position for range check
		Vector2 towerPosition = GetParent<Node2D>()?.GlobalPosition ?? GlobalPosition;
		return towerPosition.DistanceTo(player.GlobalPosition) <= PlayerActivationRange;
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		if (_isShooting)
			return;

		// Only look for targets when player is in range
		if (!IsPlayerInRange())
			return;

		var target = GetClosestEnemy();
		if (target != null)
		{
			// Face the target
			_sprite.FlipH = target.GlobalPosition.X < GlobalPosition.X;
		}
	}

	private void OnFireTimerTimeout()
	{
		// Don't shoot when paused
		if (Game.Instance.IsPaused)
			return;

		// Only shoot when player is in range
		if (!IsPlayerInRange())
			return;

		var target = GetClosestEnemy();
		if (target == null)
			return;

		// Store target position and start shoot animation
		// Arrow will be spawned when animation finishes
		_pendingTargetPosition = target.GlobalPosition;
		_isShooting = true;
		_sprite.Play("shoot");
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "shoot")
		{
			// Spawn arrow at the end of the shoot animation
			if (_pendingTargetPosition.HasValue)
			{
				SpawnArrow(_pendingTargetPosition.Value);
				_pendingTargetPosition = null;
			}

			_isShooting = false;
			_sprite.Play("idle");
		}
	}

	private void SpawnArrow(Vector2 targetPosition)
	{
		if (ArrowScene == null)
			return;

		var arrow = ArrowScene.Instantiate<TowerArrow>();
		GetTree().CurrentScene.AddChild(arrow);
		arrow.GlobalPosition = GlobalPosition;

		Vector2 direction = (targetPosition - GlobalPosition).Normalized();
		arrow.SetDirection(direction);
	}

	private Node2D GetClosestEnemy()
	{
		Node2D closestEnemy = null;
		float closestDistance = DetectionRange * DetectionRange;

		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);

		foreach (Node2D enemy in enemies)
		{
			float distanceSq = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
			if (distanceSq < closestDistance)
			{
				closestDistance = distanceSq;
				closestEnemy = enemy;
			}
		}

		return closestEnemy;
	}
}
