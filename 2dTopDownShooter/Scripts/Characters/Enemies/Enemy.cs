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
	private const short _distanceDelta = 60;
	private const ushort BaseHealth = 100;

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
	private bool withinRange = false;
	private short animationFinishedCount = 0;

	private float attackSpeed = 1f;
	private float timeUntilNextAttack;

	private Player player;

	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	public override void _Ready()
	{
		timeUntilNextAttack = attackSpeed;
		player = Game.Instance.Player;
		_animation = GetNode<AnimatedSprite2D>("EnemyAnimations");

		// Initialize health and sprite based on tier (set before _Ready via InitializeSpawnedObject)
		Health = (ushort)(BaseHealth * (int)Tier);
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

		if (withinRange && timeUntilNextAttack <= 0)
		{
			Attack();
			timeUntilNextAttack = attackSpeed;
			return;
		}

		timeUntilNextAttack -= (float)delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Game.Instance.IsPaused)
			return;

		var distanceFromPlayer = player.GlobalPosition - GlobalPosition;
		Vector2 move_input = distanceFromPlayer.Normalized();
		Velocity = move_input * Speed;
		_animation.FlipH = move_input.X < 0;

		MoveAndSlide();
	}

	private void Attack()
	{
		var distanceFromPlayer = player.GlobalPosition - GlobalPosition;
		string animationName;
		if (distanceFromPlayer.Y < -_distanceDelta)
		{
			animationName = "attack_up";

		}
		else if (distanceFromPlayer.Y > _distanceDelta)
		{
			animationName = "attack_down";
		}
		else
		{
			animationName = "attack";
		}

		_animation.Play(animationName);
		Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, Damage);
		_animation.AnimationFinished += () => Idle(animationName);
	}

	private void Idle(string animationName)
	{
		if (animationFinishedCount > 0)
		{
			_animation.Play("idle");
		}
		else
		{
			_animation.Play(animationName);
			animationFinishedCount++;
		}
	}

	public void OnAttackRangeEntered(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			withinRange = true;
		}
	}

	public void OnAttackRangeExit(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			_animation.Play("walk");
			withinRange = false;
			timeUntilNextAttack = attackSpeed;
		}
	}
}
