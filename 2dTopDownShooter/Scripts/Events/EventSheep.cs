using dTopDownShooter.Scripts;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class EventSheep : CharacterBody2D
	{
		[Signal]
		public delegate void CollectedEventHandler(EventSheep sheep);

		public bool IsCollected { get; private set; }

		private const float FollowDistance = 70f;
		private const float FollowCatchUpDistance = 120f;
		private const float MoveSpeed = 150f;

		private AnimatedSprite2D _sprite;
		private bool _delivered;

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

			var direction = player.GlobalPosition - GlobalPosition;
			var distance = direction.Length();
			if (distance <= FollowDistance)
			{
				Velocity = Vector2.Zero;
				_sprite?.Play("idle");
				return;
			}

			var speedScale = distance > FollowCatchUpDistance ? 1.25f : 1f;
			Velocity = direction.Normalized() * MoveSpeed * speedScale;
			MoveAndSlide();

			if (_sprite != null)
			{
				_sprite.Play("walk");
				_sprite.FlipH = Velocity.X < 0;
			}
		}

		public void Collect()
		{
			if (IsCollected || _delivered)
				return;

			IsCollected = true;
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
