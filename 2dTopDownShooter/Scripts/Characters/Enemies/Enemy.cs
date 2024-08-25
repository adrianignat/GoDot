using dTopDownShooter.Scripts.Characters;
using dTopDownShooter.Scripts.Spawners;
using Godot;
using System;

public partial class Enemy : Character
{
	private const short _distanceDelta = 60;

	[Export]
	private short Damage = 10;
	private bool withinRange = false;
	private short animationFinishedCount = 0;

	private float attackSpeed = 1f;
	private float timeUntilNextAttack;

	private Player player;
	private LootSpawner lootSpawner;
	private AnimatedSprite2D _enemyAnimation;

	public override void _Ready()
	{
		timeUntilNextAttack = attackSpeed;
		player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
		lootSpawner = GetTree().Root.GetNode("main").GetNode<LootSpawner>("LootSpawner");
		_enemyAnimation = GetNode<AnimatedSprite2D>("EnemyAnimations");
		_enemyAnimation.Play("walk");

		if (player == null || player.IsDead)
			return;
		Killed += OnKilled;
	}

	private void OnKilled()
	{
		player.UpdateScore();
		DropXP();
		QueueFree();
	}

	private void DropXP()
	{
		lootSpawner.Spawn(GlobalPosition);
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
