using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Ranged goblin enemy that stops at a distance and throws dynamite at the player.
/// </summary>
public partial class TntGoblin : Enemy
{
	private static readonly Dictionary<EnemyTier, string> TierSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Goblins/TNT/Blue/TNT_Blue.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Goblins/TNT/Red/TNT_Red.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Goblins/TNT/Purple/TNT_Purple.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Goblins/TNT/Yellow/TNT_Yellow.png" }
	};

	// Cached SpriteFrames per tier
	private static Dictionary<EnemyTier, SpriteFrames> TierSpriteFramesCache = new();
	private static bool _cacheCleared = false;

	[Export]
	public float ThrowRange = 150f;

	[Export]
	public float ThrowCooldown = 1.5f;

	[Export]
	public float ThrowAnimationDelay = 0.25f;

	private bool _isWithinThrowRange = false;
	private bool _isThrowing = false;
	private float _timeUntilNextThrow;
	private float _throwDelayTimer = -1f;
	private PackedScene _dynamiteScene;
	private Vector2 _throwTargetPosition;

	protected override void ConfigureEnemy()
	{
		_animationNodeName = "TntGoblinAnimations";

		// Clear cache once per game session to ensure fresh animation data
		if (!_cacheCleared)
		{
			TierSpriteFramesCache.Clear();
			_cacheCleared = true;
		}
	}

	protected override void OnReady()
	{
		_timeUntilNextThrow = 0; // Throw immediately when first in range
		_dynamiteScene = GD.Load<PackedScene>("res://Entities/Projectiles/enemy_dynamite.tscn");
	}

	protected override void LoadSpriteForTier()
	{
		if (!TierSpritePaths.TryGetValue(Tier, out var spritePath))
			return;

		// Use cached SpriteFrames or create and cache if first time
		if (!TierSpriteFramesCache.TryGetValue(Tier, out var spriteFrames))
		{
			var texture = GD.Load<Texture2D>(spritePath);
			if (texture == null)
				return;

			spriteFrames = new SpriteFrames();
			var animations = new (string name, int row, int frameCount, bool loop, float speed)[]
			{
				("idle", 0, 6, true, 10.0f),    // Ready stance (row 0)
				("walk", 1, 6, true, 10.0f),    // Walking (row 1)
				("attack", 2, 7, false, 10.0f)  // Attack/throw motion (row 2)
			};
			BuildAnimationFrames(spriteFrames, texture, animations);
			TierSpriteFramesCache[Tier] = spriteFrames;
		}

		_animation.SpriteFrames = spriteFrames;
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		// Check distance to player
		float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		_isWithinThrowRange = distanceToPlayer <= ThrowRange;

		// Handle throw delay timer (pause-aware)
		if (_throwDelayTimer > 0)
		{
			_throwDelayTimer -= (float)delta;
			if (_throwDelayTimer <= 0)
			{
				SpawnDynamite();
				_throwDelayTimer = -1f;
			}
		}

		// Cooldown always counts down (even while chasing)
		if (_timeUntilNextThrow > 0)
		{
			_timeUntilNextThrow -= (float)delta;
		}

		// Throw when in range, not on cooldown, and not already throwing
		if (_isWithinThrowRange && !_isThrowing && _timeUntilNextThrow <= 0)
		{
			ThrowDynamite();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		// Don't move while throwing
		if (_isThrowing)
			return;

		var distanceFromPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = distanceFromPlayer.Length();

		// If within throw range, stop and idle
		if (distance <= ThrowRange)
		{
			Velocity = Vector2.Zero;
			if (_animation.Animation != "idle" && _animation.Animation != "attack")
			{
				_animation.Play("idle");
			}
		}
		else
		{
			// Move toward player
			Vector2 moveDirection = distanceFromPlayer.Normalized();
			Velocity = moveDirection * Speed;
			_animation.FlipH = moveDirection.X < 0;

			if (_animation.Animation != "walk")
			{
				_animation.Play("walk");
			}
		}

		MoveAndSlide();
	}

	private void ThrowDynamite()
	{
		_isThrowing = true;
		_animation.Play("attack");

		// Calculate and store target position NOW (not when dynamite spawns)
		// This prevents the dynamite from tracking player movement during throw animation
		Vector2 directionToPlayer = _player.GlobalPosition - GlobalPosition;
		float distanceToPlayer = directionToPlayer.Length();

		// Clamp to throw range - dynamite can't go further than ThrowRange
		if (distanceToPlayer > ThrowRange)
		{
			_throwTargetPosition = GlobalPosition + directionToPlayer.Normalized() * ThrowRange;
		}
		else
		{
			_throwTargetPosition = _player.GlobalPosition;
		}

		// Spawn dynamite at the right moment (middle of throw animation) - pause-aware
		_throwDelayTimer = ThrowAnimationDelay;
	}

	private void SpawnDynamite()
	{
		if (!IsInsideTree())
			return;

		var dynamite = _dynamiteScene.Instantiate<EnemyDynamite>();
		dynamite.StartPosition = GlobalPosition;
		dynamite.TargetPosition = _throwTargetPosition; // Use stored position, not current player position

		GetTree().Root.AddChild(dynamite);
	}

	protected override void OnAnimationFinished()
	{
		if (_animation.Animation == "attack")
		{
			_isThrowing = false;
			_timeUntilNextThrow = ThrowCooldown;

			// Return to idle or walk based on range
			if (_isWithinThrowRange)
			{
				_animation.Play("idle");
			}
			else
			{
				_animation.Play("walk");
			}
		}
	}
}
