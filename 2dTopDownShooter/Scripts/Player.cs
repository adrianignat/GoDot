using dTopDownShooter.Scripts;
using Godot;

public partial class Player : CharacterBody2D
{
	private const float SPEED = 300.0f;
	private AnimatedSprite2D playerAnimation;

	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }

	public override void _Ready()
	{
		playerAnimation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		playerAnimation.Play("idle");
		Facing = Direction.E;
		Moving = Direction.E;
	}

	public override void _Process(double _delta)
	{
		var move_input = Input.GetVector("left", "right", "up", "down");
		var isPlayerShooting = playerAnimation.Animation.ToString().Contains("shoot");

		//Facing = inputVector.X < 0 ? Direction.Left : inputVector.X > 0 ? Direction.Right : Facing;
		//Moving = inputVector.X < 0 ? Direction.Left : inputVector.X > 0 ? Direction.Right : inputVector.Y < 0 ? Direction.Up : inputVector.Y > 0 ? Direction.Down : Facing;

		if (move_input.X < 0)
			Facing = Direction.W;
		else if (move_input.X > 0)
			Facing = Direction.E;

		if (move_input.X > 0 && move_input.Y == 0) // Right
			Moving = Direction.E;
		else if (move_input.X < 0 && move_input.Y == 0) // Left
			Moving = Direction.W;
		else if (move_input.Y < 0 && move_input.X == 0) // Up
			Moving = Direction.N;
		else if (move_input.Y > 0 && move_input.X == 0) // Down
			Moving = Direction.S;
		else if (move_input.X > 0 && move_input.Y > 0) // Down Right
			Moving = Direction.SE;
		else if (move_input.X > 0 && move_input.Y < 0) // Up Right
			Moving = Direction.NE;
		else if (move_input.X < 0 && move_input.Y > 0) // Down Left
			Moving = Direction.SW;
		else if (move_input.X < 0 && move_input.Y < 0) // Up Left
			Moving = Direction.NW;

		if (isPlayerShooting)
		{
			return;
		}

		if (move_input != Vector2.Zero)
		{
			playerAnimation.Play("walk");
		}
		else
		{
			Moving = Facing;
			playerAnimation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * SPEED;
		playerAnimation.FlipH = Facing == Direction.W;
		MoveAndSlide();
	}
}
