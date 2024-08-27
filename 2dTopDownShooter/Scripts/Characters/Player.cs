using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;


public partial class Player : Character
{
	private ushort _gold = 0;
	private ushort _score = 0;
	private Label _scoreLabel;

	public bool IsShooting = false;

	private AnimatedSprite2D _playerAnimation;
	public Direction Facing { get; private set; }
	public Direction Moving { get; private set; }


	public override void _Ready()
	{
		_playerAnimation = GetNode<AnimatedSprite2D>("PlayerAnimations");
		_playerAnimation.Play("idle");
		_playerAnimation.AnimationFinished += () =>
		{
			if (IsShooting) _playerAnimation.Play("idle");
		};
		Facing = Direction.E;
		Moving = Direction.E;

		Killed += OnKilled;

		_scoreLabel = GetNode<Label>("ScoreLabel");
	}

	public override void _Process(double _delta)
	{
		if (IsDead)
			return;

		var move_input = Input.GetVector("left", "right", "up", "down");
		if (move_input != Vector2.Zero)
		{
			Facing = DirectionHelper.GetFacingDirection(move_input);
			Moving = DirectionHelper.GetMovingDirection(move_input);
		}
		IsShooting = _playerAnimation.Animation.ToString().Contains("shoot");
		if (IsShooting)
		{
			return;
		}

		if (move_input != Vector2.Zero)
		{
			_playerAnimation.Play("walk");
		}
		else
		{
			//Moving = Facing;
			_playerAnimation.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
			return;

		Vector2 move_input = Input.GetVector("left", "right", "up", "down");

		Velocity = move_input * Speed;
		_playerAnimation.FlipH = Facing == Direction.W;
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
		if (IsShooting)
			return;

		switch (Moving)
		{
			case Direction.N:
				_playerAnimation.Play("shoot_up");
				break;
			case Direction.S:
				_playerAnimation.Play("shoot_down");
				break;
			case Direction.SE:
			case Direction.SW:
				_playerAnimation.Play("shoot_down_forward");
				break;
			case Direction.NE:
			case Direction.NW:
				_playerAnimation.Play("shoot_up_forward");
				break;
			default:
				_playerAnimation.Play("shoot_forward");
				break;
		}
	}

	private void OnKilled()
	{
		_playerAnimation.Play("dead");
	}
}
