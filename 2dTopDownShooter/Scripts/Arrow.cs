using Godot;

public partial class Arrow : RigidBody2D
{
	public override void _Ready()
	{
		var timer = GetNode<Timer>("Timer");
		timer.Timeout += () => QueueFree();
	}
}
