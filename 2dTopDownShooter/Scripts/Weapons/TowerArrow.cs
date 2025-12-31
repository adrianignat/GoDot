using dTopDownShooter.Scripts;
using Godot;

public partial class TowerArrow : Area2D
{
	[Export] public ushort Damage { get; set; } = 100;
	[Export] public float Speed { get; set; } = 600f;

	private Vector2 _direction;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();
	}

	public void SetDirection(Vector2 direction)
	{
		_direction = direction.Normalized();
		Rotation = _direction.Angle();
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += _direction * Speed * (float)delta;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Enemy enemy)
		{
			enemy.TakeDamage(Damage);
			QueueFree();
		}
	}
}
