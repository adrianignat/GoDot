using dTopDownShooter.Scripts;
using Godot;

public partial class Dynamite : Area2D
{
	[Export]
	public ushort Damage { get; set; } = 100;

	[Export]
	public float FuseTime { get; set; } = 1f;

	[Export]
	public float BlastRadius { get; set; } = 100f;

	private Sprite2D _sprite;
	private float _fuseTimeRemaining;
	private bool _hasExploded = false;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite");

		// Initialize fuse timer (pause-aware)
		_fuseTimeRemaining = FuseTime;
	}

	public override void _Process(double delta)
	{
		if (_hasExploded)
			return;

		// Don't process when game is paused
		if (Game.Instance.IsPaused)
			return;

		// Count down fuse timer
		_fuseTimeRemaining -= (float)delta;
		if (_fuseTimeRemaining <= 0)
		{
			Explode();
			return;
		}

		// Flicker effect as fuse burns
		if (_fuseTimeRemaining < 0.5f)
		{
			_sprite.Modulate = new Color(1, 1, 1, (float)(0.5f + 0.5f * Mathf.Sin(_fuseTimeRemaining * 30)));
		}
	}

	private void Explode()
	{
		_hasExploded = true;

		// Get all enemies in blast radius
		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node2D enemy in enemies)
		{
			float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
			if (distance <= BlastRadius)
			{
				if (enemy is Enemy enemyChar)
				{
					enemyChar.TakeDamage(Damage);
				}
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
		tween.TweenProperty(_sprite, "scale", Vector2.One * 3, 0.2f);
		tween.TweenProperty(_sprite, "modulate", new Color(1, 0.5f, 0, 1), 0.1f);
		tween.SetParallel(false);
		tween.TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0), 0.1f);
		tween.TweenCallback(Callable.From(() => QueueFree()));
	}
}
