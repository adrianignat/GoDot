using dTopDownShooter.Scripts;
using Godot;

public partial class TowerArcher : Node2D
{
	[Export] public PackedScene ArrowScene;
	[Export] public float FireRate = 1.5f; // arrows per second
	[Export] public float DetectionRange = 400f;

	private AnimatedSprite2D _sprite;
	private Timer _fireTimer;
	private bool _isShooting = false;
	private bool _playerInRange = false;
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

	public void OnPlayerActivationEntered(Node2D body)
	{
		if (body.IsInGroup(GameConstants.PlayerGroup))
			_playerInRange = true;
	}

	public void OnPlayerActivationExited(Node2D body)
	{
		if (body.IsInGroup(GameConstants.PlayerGroup))
			_playerInRange = false;
	}

	public override void _Process(double delta)
	{
		if (_isShooting || !_playerInRange)
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
		if (!_playerInRange)
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
