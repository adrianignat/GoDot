using Godot;

public partial class Arrow : RigidBody2D
{
	public short Damage { get; internal set; }

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Timer");
		timer.Timeout += () => QueueFree();
		ContactMonitor = true;
		MaxContactsReported = 1;
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
