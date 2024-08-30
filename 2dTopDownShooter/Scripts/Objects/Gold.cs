using dTopDownShooter.Scripts;
using Godot;

public partial class Gold : Area2D
{
	[Export]
	public ushort Amount { get; set; }

	public override void _Ready()
	{
		var animation = GetNode<AnimatedSprite2D>("GoldAnimation");
		animation.Play("spawn");
	}

	private void OnCollision(Node body)
	{
		if (body.IsInGroup("player"))
		{
			Game.Instance.EmitSignal(Game.SignalName.GoldAcquired, Amount);
			QueueFree();
		}
	}
}
