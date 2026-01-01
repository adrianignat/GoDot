using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;

public partial class Barrel : Area2D
{
	[Export]
	public ushort Damage { get; set; } = 100;

	[Export]
	public float ThrowDuration { get; set; } = 0.4f;

	[Export]
	public float BlastRadius { get; set; } = 100f;

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

		// Add to dynamite group for cleanup on restart
		AddToGroup(GameConstants.DynamiteGroup);

		// Set starting position and begin throw
		GlobalPosition = StartPosition;
		StartThrow();
	}

	public override void _Process(double delta)
	{
		// Handle fuse timer
		if (_fuseStarted && !_hasExploded)
		{
			_fuseTimer -= (float)delta;
			if (_fuseTimer <= 0)
			{
				Explode();
			}
		}
	}

	public override void _Draw()
	{
		if (_showBlastRadius)
		{
			// Draw subtle cyan circle showing blast radius (distinct from enemy red)
			DrawArc(Vector2.Zero, BlastRadius, 0, Mathf.Tau, 32, new Color(0, 1, 1, 0.25f), 1f);
		}
	}

	private void StartThrow()
	{
		// Play throw animation while flying
		_animation.Play("throw");

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

	private void StartFuse()
	{
		// Reset rotation
		_animation.Rotation = 0;

		// Show blast radius indicator
		_showBlastRadius = true;
		QueueRedraw();

		// Play the fuse/ignite animation
		_animation.Play("fuse");

		// Start fuse timer (pause-aware, replaces animation event)
		_fuseStarted = true;
		_fuseTimer = FuseTime;
	}

	private void Explode()
	{
		if (_hasExploded)
			return;

		_hasExploded = true;

		// Hide blast radius indicator when explosion happens
		_showBlastRadius = false;
		QueueRedraw();

		// Get all enemies in blast radius
		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);
		foreach (Node2D enemy in enemies)
		{
			float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
			if (distance <= BlastRadius)
			{
				if (enemy is Character enemyChar)
				{
					enemyChar.TakeDamage(Damage);
				}
			}
		}

		QueueFree();
	}
}
