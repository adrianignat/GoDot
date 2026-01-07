using dTopDownShooter.Scripts;
using Godot;

/// <summary>
/// Projectile fired by Shaman enemies that homes in on the player.
/// Similar to Arrow but slower and targets the player instead of enemies.
/// </summary>
public partial class ShamanProjectile : RigidBody2D
{
	[Export]
	public ushort Damage { get; set; } = GameConstants.ShamanProjectileDamage;

	[Export]
	public float Speed { get; set; } = GameConstants.ShamanProjectileSpeed;

	private Player _player;
	private Vector2 _direction;
	private AnimatedSprite2D _animation;
	private bool _hasHit = false;

	public override void _Ready()
	{
		var timer = GetNode<Timer>("Lifespan");
		timer.Timeout += () => QueueFree();

		ContactMonitor = true;
		MaxContactsReported = 1;

		_player = Game.Instance.Player;
		_animation = GetNode<AnimatedSprite2D>("ProjectileAnimations");

		// Calculate direction toward player at spawn time
		_direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		LinearVelocity = _direction * Speed;
		Rotation = _direction.Angle();

		_animation.Play("fly");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hasHit)
			return;

		// Maintain speed and direction
		LinearVelocity = _direction * Speed;
		AngularVelocity = 0;
		Rotation = _direction.Angle();
	}

	private void OnCollision(Node body)
	{
		if (_hasHit)
			return;

		if (body is Player)
		{
			Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, Damage);
			PlayExplosionAndDestroy();
		}
		else if (body is not Enemy)
		{
			// Hit something that's not a player or enemy (wall, etc.)
			PlayExplosionAndDestroy();
		}
		// Pass through other enemies without effect
	}

	private void PlayExplosionAndDestroy()
	{
		_hasHit = true;
		LinearVelocity = Vector2.Zero;

		// Disable collision
		GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);

		_animation.Play("explode");
		_animation.AnimationFinished += () => QueueFree();
	}
}
