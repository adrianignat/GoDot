using Godot;

/// <summary>
/// Simple animated sheep decoration that plays its idle animation.
/// </summary>
public partial class Sheep : AnimatedSprite2D
{
	public override void _Ready()
	{
		Play("idle");
	}
}
