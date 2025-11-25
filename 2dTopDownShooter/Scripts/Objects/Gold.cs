using dTopDownShooter.Scripts;
using Godot;

public partial class Gold : Area2D
{
	[Export]
	public ushort Amount { get; set; }

	[Export]
	public float MagnetSpeed { get; set; } = 300f;

	private bool _isInMagnetZone = false;
	private Player _player;

	public override void _Ready()
	{
		var animation = GetNode<AnimatedSprite2D>("GoldAnimation");
		animation.Play("spawn");

		// Add to gold group for cleanup on day transition
		AddToGroup("gold");

		// Get player reference
		_player = Game.Instance.Player;

		// Connect area signals for magnet detection
		AreaEntered += OnMagnetAreaEntered;
		AreaExited += OnMagnetAreaExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isInMagnetZone && _player != null)
		{
			// Move toward player
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += direction * MagnetSpeed * (float)delta;
		}
	}

	private void OnMagnetAreaEntered(Area2D area)
	{
		if (area.Name == "GoldMagnetArea")
		{
			_isInMagnetZone = true;
		}
	}

	private void OnMagnetAreaExited(Area2D area)
	{
		if (area.Name == "GoldMagnetArea")
		{
			_isInMagnetZone = false;
		}
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
