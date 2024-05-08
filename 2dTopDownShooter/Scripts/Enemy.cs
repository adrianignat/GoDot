using Godot;

public partial class Enemy : CharacterBody2D
{
	private const short _distanceDelta = 60;

	[Export]
	private float Speed = 100.0f;
	[Export]
	private short Health = 100;
	[Export]
	private short Damage = 10;
	private bool withinRange = false;
	private short animationFinishedCount = 0;

	private float attackSpeed = 1f;
	private float timeUntilNextAttack;

	private Player player;
	private AnimatedSprite2D _enemyAnimation;

	[Signal]
	public delegate void KilledEventHandler();

	public override void _Ready()
	{
		timeUntilNextAttack = attackSpeed;
		player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
		_enemyAnimation = GetNode<AnimatedSprite2D>("EnemyAnimations");
		_enemyAnimation.Play("walk");

		if (player == null || player.IsDead)
			return;
		Killed += player.UpdateScore;
	}

	public override void _Process(double delta)
	{
		if (player == null || player.IsDead)
			return;
	   
		if (withinRange && timeUntilNextAttack <= 0)
		{
			Attack();
			timeUntilNextAttack = attackSpeed;
			return;
		}

		timeUntilNextAttack -= (float)delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player == null || player.IsDead)
			return;

		var distanceFromPlayer = player.GlobalPosition - GlobalPosition;
		Vector2 move_input = distanceFromPlayer.Normalized();
		Velocity = move_input * Speed;
		_enemyAnimation.FlipH = move_input.X < 0;

		MoveAndSlide();
	}

	public void TakeDamage(short damage)
	{
		Health -= damage;
		if (Health <= 0)
		{
			EmitSignal("Killed");
			QueueFree();
		}
	}

	private void Attack()
	{
		if (player == null || player.IsDead)
			return;

		var distanceFromPlayer = player.GlobalPosition - GlobalPosition;
		string animationName;
		if (distanceFromPlayer.Y < -_distanceDelta)
		{
			animationName = "attack_up";
			
		}
		else if (distanceFromPlayer.Y > _distanceDelta)
		{
			animationName = "attack_down";
		}
		else
		{
			animationName = "attack";
		}

		_enemyAnimation.Play(animationName);
		player.TakeDamage(Damage);
		_enemyAnimation.AnimationFinished += () => Idle(animationName);
	}

	private void Idle(string animationName)
	{
		if (animationFinishedCount > 0)
		{
			_enemyAnimation.Play("idle");
		}
		else
		{
			_enemyAnimation.Play(animationName);
			animationFinishedCount++;
		}
	}

	public void OnAttackRangeEntered(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			withinRange = true;
		}
	}

	public void OnAttackRangeExit(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			_enemyAnimation.Play("walk");
			withinRange = false;
			timeUntilNextAttack = attackSpeed;
		}
	}
}
