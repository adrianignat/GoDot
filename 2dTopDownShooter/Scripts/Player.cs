using dTopDownShooter.Scripts;
using Godot;

public partial class Player : CharacterBody2D
{
	private const float _speed = 300.0f;
	private AnimatedSprite2D _playerAnimation;

	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }

	public override void _Ready()
	{
		_playerAnimation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		_playerAnimation.Play("idle");
		Facing = Direction.E;
		Moving = Direction.E;
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
			Moving = Facing;
			_playerAnimation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * _speed;
		_playerAnimation.FlipH = Facing == Direction.W;
		MoveAndSlide();
	}
}
