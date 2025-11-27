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

	public Vector2 StartPosition { get; set; }
	public Vector2 TargetPosition { get; set; }

	private AnimatedSprite2D _animation;
	private bool _hasExploded = false;
	private bool _showBlastRadius = false;

	public override void _Ready()
	{
		_animation = GetNode<AnimatedSprite2D>("Animation");

		// Add to dynamite group for cleanup on restart
		AddToGroup(GameConstants.DynamiteGroup);

		// Set starting position and begin throw
		GlobalPosition = StartPosition;
		StartThrow();
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
		// Reset rotation
		_animation.Rotation = 0;

		// Show blast radius indicator
		_showBlastRadius = true;
		QueueRedraw();

		// Play the fuse/ignite animation
		_animation.Play("fuse");

		// When animation finishes, explode
		_animation.AnimationFinished += OnFuseFinished;
	}

	private void OnFuseFinished()
	{
		_animation.AnimationFinished -= OnFuseFinished;
		Explode();
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
