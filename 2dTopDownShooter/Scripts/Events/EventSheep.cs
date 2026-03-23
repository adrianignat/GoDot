using dTopDownShooter.Scripts;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class EventSheep : CharacterBody2D
	{
		[Signal]
		public delegate void CollectedEventHandler(EventSheep sheep);

		public bool IsCollected { get; private set; }

		private const float StopDistance = 58f;
		private const float ResumeDistance = 92f;
		private const float FollowCatchUpDistance = 140f;
		private const float MoveSpeed = 145f;

		private AnimatedSprite2D _sprite;
		private bool _delivered;
		private bool _isMovingToPlayer;
		private bool _lastFlipH;

		public override void _Ready()
		{
			_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		}

		public override void _PhysicsProcess(double delta)
		{
			if (_delivered)
				return;

			if (!IsCollected)
			{
				Velocity = Vector2.Zero;
				_sprite?.Play("idle");
				return;
			}

			var player = Game.Instance.Player;
			if (player == null)
				return;

			var toPlayer = player.GlobalPosition - GlobalPosition;
			var distance = toPlayer.Length();

			if (_isMovingToPlayer)
			{
				if (distance <= StopDistance)
				{
					_isMovingToPlayer = false;
					Velocity = Vector2.Zero;
					if (_sprite != null)
					{
						_sprite.Play("idle");
						_sprite.FlipH = _lastFlipH;
					}
					return;
				}
			}
			else
			{
				if (distance <= ResumeDistance)
				{
					Velocity = Vector2.Zero;
					if (_sprite != null)
					{
						_sprite.Play("idle");
						_sprite.FlipH = _lastFlipH;
					}
					return;
				}
				_isMovingToPlayer = true;
			}

			var speedScale = distance > FollowCatchUpDistance ? 1.15f : 1f;
			Velocity = toPlayer.Normalized() * MoveSpeed * speedScale;
			MoveAndSlide();

			if (_sprite != null)
			{
				_sprite.Play("walk");
				if (Mathf.Abs(Velocity.X) > 5f)
					_lastFlipH = Velocity.X < 0;
				_sprite.FlipH = _lastFlipH;
			}
		}

		public void Collect()
		{
			if (IsCollected || _delivered)
				return;

			IsCollected = true;
			_isMovingToPlayer = true;
			EmitSignal(SignalName.Collected, this);
		}

		public void MarkDelivered()
		{
			_delivered = true;
			Visible = false;
			SetPhysicsProcess(false);
			QueueFree();
		}
	}
}
