using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Ranged enemy that shoots homing projectiles at the player.
/// Maintains distance and casts spells from range.
/// </summary>
public partial class Shaman : Enemy
{
	private const string SpritePath = "res://Sprites/Characters/Enemies/Shaman/";

	// Cached SpriteFrames - created once and reused
	private static SpriteFrames _cachedSpriteFrames;

	[Export]
	private float AttackCooldown = 2.0f;

	[Export]
	private float PreferredDistance = 200f;

	private const int ProjectileSpawnFrame = 7; // 0-indexed, so frame 8

	private PackedScene _projectileScene;
	private bool _isAttacking = false;
	private bool _projectileSpawned = false;
	private float _timeUntilNextAttack;

	protected override void OnReady()
	{
		_projectileScene = GD.Load<PackedScene>("res://Entities/Projectiles/shaman_projectile.tscn");
		// Randomize initial attack time to stagger attacks across multiple shamans
		_timeUntilNextAttack = (float)GD.RandRange(0, AttackCooldown);

		_animation.FrameChanged += OnFrameChanged;
	}

	private void OnFrameChanged()
	{
		// Spawn projectile on frame 8 (index 7) of attack animation
		if (_animation.Animation == "attack" && _animation.Frame == ProjectileSpawnFrame && !_projectileSpawned)
		{
			SpawnProjectile();
			_projectileSpawned = true;
		}
	}

	protected override void LoadSpriteForTier()
	{
		// Use cached SpriteFrames or create if first time
		// Shaman only has one color variant (not tier-based sprites)
		if (_cachedSpriteFrames == null)
		{
			_cachedSpriteFrames = new SpriteFrames();

			var idleTexture = GD.Load<Texture2D>(SpritePath + "Shaman_Idle.png");
			var runTexture = GD.Load<Texture2D>(SpritePath + "Shaman_Run.png");
			var attackTexture = GD.Load<Texture2D>(SpritePath + "Shaman_Attack.png");

			// Build idle animation (8 frames, single row)
			BuildSingleRowAnimation(_cachedSpriteFrames, idleTexture, "idle", 8, true, 10.0f);

			// Build walk animation (4 frames, single row)
			BuildSingleRowAnimation(_cachedSpriteFrames, runTexture, "walk", 4, true, 10.0f);

			// Build attack animation (10 frames, single row)
			BuildSingleRowAnimation(_cachedSpriteFrames, attackTexture, "attack", 10, false, 12.0f);
		}

		_animation.SpriteFrames = _cachedSpriteFrames;
	}

	private static void BuildSingleRowAnimation(
		SpriteFrames spriteFrames,
		Texture2D texture,
		string animName,
		int frameCount,
		bool loop,
		float speed)
	{
		spriteFrames.AddAnimation(animName);
		spriteFrames.SetAnimationLoop(animName, loop);
		spriteFrames.SetAnimationSpeed(animName, speed);

		// Calculate frame size from texture width
		int frameWidth = (int)texture.GetWidth() / frameCount;
		int frameHeight = (int)texture.GetHeight();

		for (int i = 0; i < frameCount; i++)
		{
			var atlasTexture = new AtlasTexture();
			atlasTexture.Atlas = texture;
			atlasTexture.Region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight);
			spriteFrames.AddFrame(animName, atlasTexture);
		}
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		if (!_isAttacking)
		{
			_timeUntilNextAttack -= (float)delta;

			if (_timeUntilNextAttack <= 0 && CanSeePlayer())
			{
				Attack();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		// Don't move while attacking
		if (_isAttacking)
			return;

		var distanceFromPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = distanceFromPlayer.Length();
		Vector2 moveDirection;

		if (distance > PreferredDistance + 50)
		{
			// Move toward player if too far
			moveDirection = distanceFromPlayer.Normalized();
		}
		else if (distance < PreferredDistance - 50)
		{
			// Move away from player if too close
			moveDirection = -distanceFromPlayer.Normalized();
		}
		else
		{
			// At preferred distance, stop moving
			moveDirection = Vector2.Zero;
		}

		Velocity = moveDirection * Speed;
		_animation.FlipH = distanceFromPlayer.X < 0;

		MoveAndSlide();
	}

	private bool CanSeePlayer()
	{
		// Check if player is within reasonable range
		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		return distance < 400f;
	}

	private void Attack()
	{
		_isAttacking = true;
		_projectileSpawned = false;
		_animation.Play("attack");
	}

	protected override void OnAnimationFinished()
	{
		if (_animation.Animation == "attack")
		{
			// Projectile was already spawned on frame 8 via OnFrameChanged
			_isAttacking = false;
			_timeUntilNextAttack = AttackCooldown;
			_animation.Play("walk");
		}
	}

	private void SpawnProjectile()
	{
		if (_projectileScene == null)
			return;

		var projectile = _projectileScene.Instantiate<ShamanProjectile>();
		projectile.GlobalPosition = GlobalPosition;

		Game.Instance.EntityLayer.AddChild(projectile);
	}
}
