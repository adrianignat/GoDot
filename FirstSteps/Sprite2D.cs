using Godot;
using System;

public partial class Sprite2D : Godot.Sprite2D
{
	private const float _angularSpeed = MathF.PI;
	private const int _speed = 1000;

	public override void _Ready()
	{
		// Called every time the node is added to the scene.
		// Initialization here
	}

	public override void _Process(double delta)
	{
		// Called every frame. Delta is the elapsed time since the previous frame.

		var velocity = Vector2.Zero;
		var facing = 1;
		if (Input.IsActionPressed("ui_up"))
		{
			facing = 1;
			velocity = Vector2.Up.Rotated(Rotation + MathF.PI / 2) * _speed;
		}
		if (Input.IsActionPressed("ui_down"))
		{
			facing = -1;
			velocity = Vector2.Down.Rotated(Rotation + MathF.PI / 2) * _speed;
		}

		var direction = 0;
		if (Input.IsActionPressed("ui_left"))
		{
			direction = -1 * facing;
		}
		if (Input.IsActionPressed("ui_right"))
		{
			direction = 1 * facing;
		}
		
		Rotation += _angularSpeed * direction * (float)delta;
		Position += velocity * (float)delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Called every physics frame. Delta is the elapsed time since the previous frame.
	}
}
