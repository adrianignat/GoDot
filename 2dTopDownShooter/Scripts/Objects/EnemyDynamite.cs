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

	[Export]
	public float FuseTime { get; set; } = 1.0f;

	public Vector2 StartPosition { get; set; }
	public Vector2 TargetPosition { get; set; }

	private AnimatedSprite2D _animation;
	private bool _hasExploded = false;
	private bool _showBlastRadius = false;
	private bool _fuseStarted = false;
	private float _fuseTimer = 0f;
	private Tween _throwTween;

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

		_throwTween = CreateTween();
		_throwTween.SetParallel(true);

		// Move to target position with arc
		_throwTween.TweenProperty(this, "global_position", TargetPosition, ThrowDuration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);

		// Rotate while flying
		_throwTween.TweenProperty(_animation, "rotation", Mathf.Tau * 2, ThrowDuration);

		_throwTween.SetParallel(false);
		_throwTween.TweenCallback(Callable.From(StartFuse));
	}

	public override void _Process(double delta)
	{
		if (_fuseStarted && !_hasExploded)
		{
			_fuseTimer -= (float)delta;
			if (_fuseTimer <= 0)
			{
				Explode();
			}
		}
	}

	private void StartFuse()
	{
		// Reset rotation after landing
		_animation.Rotation = 0;

		// Show blast radius indicator
		_showBlastRadius = true;
		QueueRedraw();

		// Start fuse timer (pause-aware)
		_fuseStarted = true;
		_fuseTimer = FuseTime;
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
