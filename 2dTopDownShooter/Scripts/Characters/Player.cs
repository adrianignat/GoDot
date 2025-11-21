using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using dTopDownShooter.Scripts.Upgrades;
using Godot;


public partial class Player : Character
{
	private ushort _gold = 0;
	private ushort _score = 0;
	private Label _scoreLabel;

	public bool IsShooting = false;
	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }


	public override void _Ready()
	{
		_animation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		_animation.Play("idle");
		_animation.AnimationFinished += () =>
		{
			if (IsShooting) _animation.Play("idle");
		};
		Facing = Direction.E;
		Moving = Direction.E;

		_scoreLabel = GetNode<Label>("ScoreLabel");

		Game.Instance.PlayerTakeDamage += TakeDamage;
		Game.Instance.GoldAcquired += AcquireGold;
		Game.Instance.UpgradeSelected += UpgradeSelected;
	}

	private void UpgradeSelected(Upgrade upgdade)
	{
		var bow = GetNode<Bow>("Bow");

		if (upgdade.Type == UpgradeType.Speed)
			Speed += upgdade.Amount;
		else if (upgdade.Type == UpgradeType.Health)
			Health += upgdade.Amount;
		else if (upgdade.Type == UpgradeType.WeaponSpeed)
			bow.ObjectsPerSecond += (upgdade.Amount / 100f);
		else if (upgdade.Type == UpgradeType.Bouncing)
		{
			bow.BouncingLevel += 1;
			bow.SelectedArrowUpgrade = UpgradeType.Bouncing;
		}
		else if (upgdade.Type == UpgradeType.Piercing)
		{
			bow.PiercingLevel += 1;
			bow.SelectedArrowUpgrade = UpgradeType.Piercing;
		}
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.IsPaused)
		{
			IsShooting = false;
			if (!IsDead)
				_animation.Play("idle");
			return;
		}

		var move_input = Input.GetVector("left", "right", "up", "down");
		if (move_input != Vector2.Zero)
		{
			Facing = DirectionHelper.GetFacingDirection(move_input);
			Moving = DirectionHelper.GetMovingDirection(move_input);
		}
		IsShooting = _animation.Animation.ToString().Contains("shoot");
		if (IsShooting)
		{
			return;
		}

		if (move_input != Vector2.Zero)
		{
			_animation.Play("walk");
		}
		else
		{
			_animation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Game.Instance.IsPaused)
		{
			IsShooting = false;
			if (!IsDead)
				_animation.Play("idle");
			return;
		}

		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * Speed;
		_animation.FlipH = Facing == Direction.W;
		MoveAndSlide();
	}

	public void UpdateScore()
	{
		_score += 1;
		_scoreLabel.Text = "Score: " + _score;
	}

	public void AcquireGold(ushort amount)
	{
		_gold += amount;
		_scoreLabel.Text = "Gold: " + _gold;
	}

	internal void PlayShootAnimation()
	{
		if (Game.Instance.IsPaused)
		{
			IsShooting = false;
			return;
		}

		if (IsShooting)
			return;

		switch (Moving)
		{
			case Direction.N:
				_animation.Play("shoot_up");
				break;
			case Direction.S:
				_animation.Play("shoot_down");
				break;
			case Direction.SE:
			case Direction.SW:
				_animation.Play("shoot_down_forward");
				break;
			case Direction.NE:
			case Direction.NW:
				_animation.Play("shoot_up_forward");
				break;
			default:
				_animation.Play("shoot_forward");
				break;
		}
	}

	internal override void OnKilled()
	{
		Game.Instance.IsPaused = true;
		_animation.Play("dead");
	}
}
