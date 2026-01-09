using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Melee goblin enemy that chases the player and attacks when in range.
/// Uses torch sprites and has directional attack animations.
/// </summary>
public partial class TorchGoblin : Enemy
{
	private static readonly Dictionary<EnemyTier, string> TierSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Goblins/Torch/Blue/Torch_Blue.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Goblins/Torch/Red/Torch_Red.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Goblins/Torch/Purple/Torch_Purple.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Goblins/Torch/Yellow/Torch_Yellow.png" }
	};

	// Cached SpriteFrames per tier - created once and reused
	private static readonly Dictionary<EnemyTier, SpriteFrames> TierSpriteFramesCache = new();

	[Export]
	private ushort Damage = GameConstants.TorchGoblinDamage;

	private bool _withinRange = false;
	private bool _isAttacking = false;
	private float _attackSpeed = GameConstants.TorchGoblinAttackSpeed;
	private float _timeUntilNextAttack;

	protected override void OnReady()
	{
		_timeUntilNextAttack = _attackSpeed;
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
				("idle", 0, 7, true, 10.0f),
				("walk", 1, 6, true, 10.0f),
				("attack", 2, 6, false, 10.0f),
				("attack_down", 3, 6, false, 10.0f),
				("attack_up", 4, 6, false, 10.0f)
			};
			BuildAnimationFrames(spriteFrames, texture, animations);
			TierSpriteFramesCache[Tier] = spriteFrames;
		}

		_animation.SpriteFrames = spriteFrames;
	}

	public override void _Process(double delta)
	{
		if (_withinRange && !_isAttacking && _timeUntilNextAttack <= 0)
		{
			Attack();
			return;
		}

		_timeUntilNextAttack -= (float)delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Don't move while attacking
		if (_isAttacking)
			return;

		//var distanceFromPlayer = _player.GlobalPosition - GlobalPosition;
		//Vector2 moveDirection = distanceFromPlayer.Normalized();

		//Mishu - AStarGrid//
		if (_path.Count == 0 || Engine.GetPhysicsFrames() % 20 == 0)
    		RecalculatePath();
		Vector2 moveDirection = GetMovementDirection();
		//Mishu - AStarGrid//

		Velocity = moveDirection * Speed;
		_animation.FlipH = moveDirection.X < 0;

		MoveAndSlide();
	}

	private void Attack()
	{
		_isAttacking = true;

		var distanceFromPlayer = _player.GlobalPosition - GlobalPosition;
		string animationName;
		if (distanceFromPlayer.Y < -GameConstants.EnemyAttackDistanceDelta)
		{
			animationName = "attack_up";
		}
		else if (distanceFromPlayer.Y > GameConstants.EnemyAttackDistanceDelta)
		{
			animationName = "attack_down";
		}
		else
		{
			animationName = "attack";
		}

		_animation.Play(animationName);
	}

	protected override void OnAnimationFinished()
	{
		// Only handle attack animation completion
		string currentAnim = _animation.Animation;
		if (currentAnim.StartsWith("attack"))
		{
			// Deal damage when attack animation completes (if still in range)
			if (_withinRange)
			{
				Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, Damage);
			}

			_isAttacking = false;
			_timeUntilNextAttack = _attackSpeed;

			// Transition to idle or walk based on range
			if (_withinRange)
			{
				_animation.Play("idle");
			}
			else
			{
				_animation.Play("walk");
			}
		}
	}

	public void OnAttackRangeEntered(Node2D body)
	{
		if (body.IsInGroup(GameConstants.PlayerGroup))
		{
			_withinRange = true;
		}
	}

	public void OnAttackRangeExit(Node2D body)
	{
		if (body.IsInGroup(GameConstants.PlayerGroup))
		{
			_withinRange = false;
			_timeUntilNextAttack = _attackSpeed;

			// Only change to walk if not currently attacking
			if (!_isAttacking)
			{
				_animation.Play("walk");
			}
		}
	}
}
