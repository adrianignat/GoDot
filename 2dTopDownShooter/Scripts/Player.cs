using Godot;

public partial class Player : CharacterBody2D
{
	private const float SPEED = 300.0f;
	private AnimatedSprite2D playerAnimation;

	public override void _Ready()
	{
		playerAnimation = GetNode<AnimatedSprite2D>("PlayerAnimations");
	}

	public override void _Process(double _delta)
	{
		if (Input.IsActionPressed("fire"))
		{
			if (Input.IsActionPressed("up"))
			{
				playerAnimation.Play("shoot_up");
			}
			else if (Input.IsActionPressed("down"))
			{
				playerAnimation.Play("shoot_down");
			}
			else
			{
				playerAnimation.Play("shoot_forward");
			}
		}
		else
		{
			if (Input.IsActionPressed("left")
				|| Input.IsActionPressed("right")
				|| Input.IsActionPressed("up")
				|| Input.IsActionPressed("down"))
			{
				playerAnimation.Play("walk");
			}
			else
			{
				playerAnimation.Play("idle");
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * SPEED;
		playerAnimation.FlipH = Velocity.X < 0;
		MoveAndSlide();
	}
}
