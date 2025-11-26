using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;
using System.Collections.Generic;

public enum EnemyTier
{
	Blue = 1,   // 1 shot to kill (100 health)
	Red = 2,    // 2 shots to kill (200 health)
	Purple = 3, // 3 shots to kill (300 health)
	Yellow = 4  // 4 shots to kill (400 health)
}

public partial class Enemy : Character
{

	private static readonly Dictionary<EnemyTier, string> TierSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Goblins/Torch/Blue/Torch_Blue.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Goblins/Torch/Red/Torch_Red.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Goblins/Torch/Purple/Torch_Purple.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Goblins/Torch/Yellow/Torch_Yellow.png" }
	};
 
	// Cached SpriteFrames per tier - created once and reused
	private static readonly Dictionary<EnemyTier, SpriteFrames> TierSpriteFramesCache = [];

	[Export]
	private ushort Damage = 10;
	private bool _withinRange = false;
	private bool _isAttacking = false;

	private float _attackSpeed = 1f;
	private float _timeUntilNextAttack;

	private Player _player;

	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	public override void _Ready()
	{
		_timeUntilNextAttack = _attackSpeed;
		_player = Game.Instance.Player;
		_animation = GetNode<AnimatedSprite2D>("EnemyAnimations");

		// Add to enemies group for cleanup on day transition
		AddToGroup(GameConstants.EnemiesGroup);

		// Connect animation finished once - handles all attack animations
		_animation.AnimationFinished += OnAnimationFinished;

		// Initialize health and sprite based on tier (set before _Ready via InitializeSpawnedObject)
		Health = (ushort)(GameConstants.EnemyBaseHealth * (int)Tier);
		LoadSpriteForTier();
		_animation.Play("walk");
	}

	private void LoadSpriteForTier()
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
			BuildAnimationFrames(spriteFrames, texture);
			TierSpriteFramesCache[Tier] = spriteFrames;
		}

		_animation.SpriteFrames = spriteFrames;
	}

	private static void BuildAnimationFrames(SpriteFrames spriteFrames, Texture2D texture)
	{
		var animations = new (string name, int row, int frameCount, bool loop, float speed)[]
		{
			("idle", 0, 7, true, 10.0f),
			("walk", 1, 6, true, 10.0f),
			("attack", 2, 6, false, 30.0f),
			("attack_down", 3, 6, false, 30.0f),
			("attack_up", 4, 6, false, 30.0f)
		};

		foreach (var (name, row, frameCount, loop, speed) in animations)
		{
			spriteFrames.AddAnimation(name);
			spriteFrames.SetAnimationLoop(name, loop);
			spriteFrames.SetAnimationSpeed(name, speed);

			for (int i = 0; i < frameCount; i++)
			{
				var atlasTexture = new AtlasTexture();
				atlasTexture.Atlas = texture;
				atlasTexture.Region = new Rect2(i * 192, row * 192, 192, 192);
				spriteFrames.AddFrame(name, atlasTexture);
			}
		}
	}

	internal override void OnKilled()
	{
		Game.Instance.EmitSignal(Game.SignalName.EnemyKilled, this);
		QueueFree();
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		if (_withinRange && !_isAttacking && _timeUntilNextAttack <= 0)
		{
			Attack();
			return;
		}

		_timeUntilNextAttack -= (float)delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		// Don't move while attacking
		if (_isAttacking)
			return;

		var distanceFromPlayer = _player.GlobalPosition - GlobalPosition;
		Vector2 moveDirection = distanceFromPlayer.Normalized();
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
		Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, Damage);
	}

	private void OnAnimationFinished()
	{
		// Only handle attack animation completion
		string currentAnim = _animation.Animation;
		if (currentAnim.StartsWith("attack"))
		{
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
