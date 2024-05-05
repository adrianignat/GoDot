using dTopDownShooter.Scripts;
using Godot;
using System.Diagnostics;

public partial class Enemy : CharacterBody2D
{
	private const short _distanceDelta = 60;
	private const float _speed = 100.0f;
	private Player player;
	private bool withinRange = false;

	private float attackSpeed = 1f;
	private float timeUntilNextAttack = 0f;

	private AnimatedSprite2D _enemyAnimation;
	private Direction _direction;

	public override void _Ready()
	{
		timeUntilNextAttack = attackSpeed;
		player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
		_enemyAnimation = GetNode<AnimatedSprite2D>("EnemyAnimations");
		_enemyAnimation.Play("walk");
	}

	public override void _Process(double delta)
	{
		if (withinRange)
		{
			if (timeUntilNextAttack <= 0)
			{
				Attack();
				timeUntilNextAttack = attackSpeed;
			}
			else
			{
				timeUntilNextAttack -= (float)delta;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
        var distanceFromPlayer = player.GlobalPosition - GlobalPosition;
        Vector2 move_input = distanceFromPlayer.Normalized();
		Velocity = move_input * _speed;
		_enemyAnimation.FlipH = move_input.X < 0;

		if (withinRange)
		{

			if (distanceFromPlayer.Y < -_distanceDelta)
			{
				_enemyAnimation.Play("attack_up");
			}
			else if (distanceFromPlayer.Y > _distanceDelta)
			{
				_enemyAnimation.Play("attack_down");
			}
			else
			{
				_enemyAnimation.Play("attack");
			}
		}

        MoveAndSlide();
	}

	private void Attack()
	{
		Debug.Print("Attacking player");
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
