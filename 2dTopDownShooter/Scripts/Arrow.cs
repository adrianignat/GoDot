using dTopDownShooter.Scripts;
using Godot;
using Godot.Collections;
using System;

public partial class Arrow : RigidBody2D
{
	[Export]
	public short Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	private Player _player;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 1;

		_player = GetTree().Root.GetNode("main").GetNode<Player>("Player");

		UpdateVelocity();
		AdjustPosition();
	}

	private void AdjustPosition()
	{
		if (_player.Facing == Direction.W
			&& _player.Moving != Direction.S
			&& _player.Moving != Direction.SW)
			Position = new Vector2(GlobalPosition.X - 50, GlobalPosition.Y);
	}

	private void UpdateRotation(Vector2 move_input)
	{
		var moving = DirectionHelper.GetMovingDirection(move_input);
		if (moving == Direction.N) // Up
			RotationDegrees = -90;
		else if (moving == Direction.S) // Down
			RotationDegrees = 90;
		else if (moving == Direction.W) // Left
			RotationDegrees = 180;
		else if (moving == Direction.E) // Right
			RotationDegrees = 0;
		else if (moving == Direction.NW) // Up Left
			RotationDegrees = -135;
		else if (moving == Direction.NE) // Up Right
			RotationDegrees = -45;
		else if (moving == Direction.SW) // Down Left
			RotationDegrees = 135;
		else if (moving == Direction.SE) // Down Right
			RotationDegrees = 45;
	}

	private void UpdateVelocity()
	{
		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		if (move_input == Vector2.Zero)
		{
			switch (_player.Moving)
			{
				case Direction.N:
					move_input = new Vector2(0, -1);
					break;
				case Direction.S:
					move_input = new Vector2(0, 1);
					break;
				case Direction.W:
					move_input = new Vector2(-1, 0);
					break;
				case Direction.E:
					move_input = new Vector2(1, 0);
					break;
				case Direction.NW:
					move_input = new Vector2(-1, -1);
					break;
				case Direction.NE:
					move_input = new Vector2(1, -1);
					break;
				case Direction.SW:
					move_input = new Vector2(-1, 1);
					break;
				case Direction.SE:
					move_input = new Vector2(1, 1);
					break;
			}
		}
		LinearVelocity = move_input * Speed;
		UpdateRotation(move_input);
	}

	private void OnCollision(Node body)
	{
		if (body is Enemy)
		{
			var enemy = body as Enemy;
			enemy.TakeDamage(Damage);
		}
		QueueFree();
	}
}
