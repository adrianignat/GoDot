using dTopDownShooter.Scripts;
using Godot;

public partial class Player : CharacterBody2D
{
	private short _score = 0;
	private Label _scoreLabel;

	[Export]
	private float Speed = 300.0f;
	[Export]
	private short Health = 100;

	public bool IsDead => Health <= 0;

	private AnimatedSprite2D _playerAnimation;
	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }

	public override void _Ready()
	{
		_playerAnimation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		_playerAnimation.Play("idle");
		Facing = Direction.E;
		Moving = Direction.E;

		//_scoreLabel = GetTree().Root.GetCamera2D().GetNode<Label>("ScoreLabel");
		_scoreLabel = GetNode<Label>("ScoreLabel");
	}

	public override void _Process(double _delta)
	{
		var move_input = Input.GetVector("left", "right", "up", "down");
		if (move_input != Vector2.Zero)
		{
			Facing = DirectionHelper.GetFacingDirection(move_input);
			Moving = DirectionHelper.GetMovingDirection(move_input);
		}
		var isPlayerShooting = _playerAnimation.Animation.ToString().Contains("shoot");
		if (isPlayerShooting)
		{
			return;
		}

		if (move_input != Vector2.Zero)
		{
			_playerAnimation.Play("walk");
		}
		else
		{
			//Moving = Facing;
			_playerAnimation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * Speed;
		_playerAnimation.FlipH = Facing == Direction.W;
		MoveAndSlide();
	}

	internal void TakeDamage(short damage)
	{
		Health -= damage;
		if (Health <= 0)
		{
			QueueFree();
		}
	}

	public void UpdateScore()
	{
		_score += 1;
		_scoreLabel.Text = "Score: " + _score;
	}
}
