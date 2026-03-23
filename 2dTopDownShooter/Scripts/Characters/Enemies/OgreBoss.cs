using dTopDownShooter.Scripts;
using Godot;
using System.Collections.Generic;

public partial class OgreBoss : Enemy
{
	private static readonly Dictionary<EnemyTier, string> TierSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Attack.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Attack.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Attack.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Attack.png" }
	};

	private static readonly Dictionary<EnemyTier, string> IdleSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Idle.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Idle.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Idle.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Idle.png" }
	};

	private static readonly Dictionary<EnemyTier, string> WalkSpritePaths = new()
	{
		{ EnemyTier.Blue, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Walk.png" },
		{ EnemyTier.Red, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Walk.png" },
		{ EnemyTier.Purple, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Walk.png" },
		{ EnemyTier.Yellow, "res://Sprites/Characters/Enemies/Minotaur/Minotaur_Walk.png" }
	};

	private static readonly Dictionary<EnemyTier, SpriteFrames> FramesCache = new();

	[Export] private ushort BossDamage = 20;
	[Export] private float BossAttackSpeed = 1.5f;
	[Export] private float BossMoveSpeed = 85f;

	private bool _withinRange;
	private bool _isAttacking;
	private float _timeUntilNextAttack;

	protected override void OnReady()
	{
		Speed = BossMoveSpeed;
		Health = 1200;
		_timeUntilNextAttack = BossAttackSpeed;
	}

	protected override void LoadSpriteForTier()
	{
		if (!FramesCache.TryGetValue(Tier, out var frames))
		{
			var attackTexture = GD.Load<Texture2D>(TierSpritePaths[Tier]);
			var idleTexture = GD.Load<Texture2D>(IdleSpritePaths[Tier]);
			var walkTexture = GD.Load<Texture2D>(WalkSpritePaths[Tier]);
			if (attackTexture == null || idleTexture == null || walkTexture == null)
				return;

			frames = new SpriteFrames();
			BuildAnimationFrames(frames, idleTexture, new[] { ("idle", 0, 16, true, 12f) }, 320);
			BuildAnimationFrames(frames, walkTexture, new[] { ("walk", 0, 8, true, 10f) }, 320);
			BuildAnimationFrames(frames, attackTexture, new[] { ("attack", 0, 12, false, 11f) }, 320);
			FramesCache[Tier] = frames;
		}

		_animation.SpriteFrames = frames;
		_animation.Scale = Vector2.One * 0.8f;
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

	protected override bool CanMove() => !_isAttacking;

	protected override void UpdateMovementAnimation(Vector2 direction)
	{
		_animation.FlipH = direction.X < 0;
		if (!_isAttacking && _animation.Animation != "walk")
			_animation.Play("walk");
	}

	private void Attack()
	{
		_isAttacking = true;
		_animation.Play("attack");
	}

	protected override void OnAnimationFinished()
	{
		if (_animation.Animation != "attack")
			return;

		if (_withinRange)
			Game.Instance.EmitSignal(Game.SignalName.PlayerTakeDamage, BossDamage);

		_isAttacking = false;
		_timeUntilNextAttack = BossAttackSpeed;
		_animation.Play(_withinRange ? "idle" : "walk");
	}

	public override void OnAttackRangeEntered(Node2D body)
	{
		if (body.IsInGroup(GameConstants.PlayerGroup))
			_withinRange = true;
	}

	public override void OnAttackRangeExit(Node2D body)
	{
		if (!body.IsInGroup(GameConstants.PlayerGroup))
			return;

		_withinRange = false;
		_timeUntilNextAttack = BossAttackSpeed;
		if (!_isAttacking)
			_animation.Play("walk");
	}
}
