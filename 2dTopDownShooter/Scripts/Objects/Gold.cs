using Godot;

public partial class Gold : Area2D
{
	[Export]
	public ushort Amount { get; set; }

	public override void _Ready()
	{
		Scale = new Vector2(0.5f, 0.5f);

		var animation = GetNode<AnimatedSprite2D>("GoldAnimation");
		animation.Play("spawn");
	}

	private void OnCollision(Node body)
	{
		if (body.IsInGroup("player"))
		{
			var player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
			player.AcquireGold(Amount);
			QueueFree();
		}
	}
}