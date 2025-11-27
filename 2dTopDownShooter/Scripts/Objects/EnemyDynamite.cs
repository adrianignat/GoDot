using dTopDownShooter.Scripts;
using Godot;

/// <summary>
/// Dynamite thrown by TNT Goblins that damages the player.
/// </summary>
public partial class EnemyDynamite : Area2D
{
	[Export]
	public ushort Damage { get; set; } = 10; // Same as goblin melee damage

	[Export]
	public float ThrowDuration { get; set; } = 0.5f;

	[Export]
	public float BlastRadius { get; set; } = 60f;

	public Vector2 StartPosition { get; set; }
	public Vector2 TargetPosition { get; set; }

	private AnimatedSprite2D _animation;
	private bool _hasExploded = false;
	private bool _showBlastRadius = false;

	public override void _Ready()
	{
		_animation = GetNode<AnimatedSprite2D>("Animation");

		// Add to dynamite group for cleanup on restart/day transition
		AddToGroup(GameConstants.DynamiteGroup);

		// Set starting position and begin throw
		GlobalPosition = StartPosition;
		StartThrow();
	}

	public override void _Draw()
	{
		if (_showBlastRadius)
		{
			// Draw subtle red circle showing blast radius
			DrawArc(Vector2.Zero, BlastRadius, 0, Mathf.Tau, 32, new Color(1, 0, 0, 0.25f), 1f);
		}
	}

	private void StartThrow()
	{
		_animation.Play("fuse");

		var tween = CreateTween();
		tween.SetParallel(true);

		// Move to target position with arc
		tween.TweenProperty(this, "global_position", TargetPosition, ThrowDuration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);

		// Rotate while flying
		tween.TweenProperty(_animation, "rotation", Mathf.Tau * 2, ThrowDuration);

		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(StartFuse));
	}

	private void StartFuse()
	{
		// Reset rotation after landing
		_animation.Rotation = 0;

		// Show blast radius indicator
		_showBlastRadius = true;
		QueueRedraw();

		// Wait 1 second with fuse burning, then explode
		var timer = GetTree().CreateTimer(1.0f);
		timer.Timeout += Explode;
	}

	private void Explode()
	{
		if (_hasExploded)
			return;

		_hasExploded = true;

		// Hide blast radius indicator
		_showBlastRadius = false;
		QueueRedraw();

		// Check if player is in blast radius
		var player = Game.Instance.Player;
		if (player != null)
		{
			float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distance <= BlastRadius)
			{
				Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, Damage);
			}
		}

		// Visual explosion effect
		PlayExplosionEffect();
	}

	private void PlayExplosionEffect()
	{
		// Scale up and fade out
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(_animation, "scale", Vector2.One * 2, 0.15f);
		tween.TweenProperty(_animation, "modulate", new Color(1, 0.5f, 0, 1), 0.08f);
		tween.SetParallel(false);
		tween.TweenProperty(_animation, "modulate", new Color(1, 1, 1, 0), 0.08f);
		tween.TweenCallback(Callable.From(() => QueueFree()));
	}
}
