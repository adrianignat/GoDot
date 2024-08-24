using Godot;

public partial class Arrow : RigidBody2D
{
	[Export]
	public short Damage { get; set; }

	[Export]
	public float Speed { get; set; }

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 1;

		Scale = new Vector2(0.5f, 0.5f);
		LinearVelocity = GetLocalMousePosition().Normalized() * Speed;
		Rotation = GetLocalMousePosition().Angle();
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
