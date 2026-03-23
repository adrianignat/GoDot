using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;

namespace dTopDownShooter.Scripts.Characters.Allies
{
	public partial class KnightAlly : Character
	{
		private const float FollowDistance = 90f;
		private const float ReengageDistance = 130f;

		private Player _player;
		private bool _isFollowing;

		public override void _Ready()
		{
			_player = Game.Instance.Player;
			_animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
			Speed = 185f;
			Health = 250;
			AddToGroup(GameConstants.AlliesGroup);
			_animation.Play("idle");
		}

		public override void _PhysicsProcess(double delta)
		{
			if (!_isFollowing || _player == null || !IsInstanceValid(_player))
			{
				Velocity = Vector2.Zero;
				if (_animation.Animation != "idle")
					_animation.Play("idle");
				return;
			}

			Vector2 deltaToPlayer = _player.GlobalPosition - GlobalPosition;
			float distance = deltaToPlayer.Length();
			if (distance <= FollowDistance)
			{
				Velocity = Vector2.Zero;
				if (_animation.Animation != "idle")
					_animation.Play("idle");
				return;
			}

			float speedScale = distance > ReengageDistance ? 1.1f : 1f;
			Velocity = deltaToPlayer.Normalized() * Speed * speedScale;
			_animation.Play("run");
			_animation.FlipH = Velocity.X < 0;
			MoveAndSlide();
		}

		public void StartFollowing()
		{
			_isFollowing = true;
		}

		public void StopFollowing()
		{
			_isFollowing = false;
			Velocity = Vector2.Zero;
			if (_animation != null)
				_animation.Play("idle");
		}

		internal override void OnKilled()
		{
			QueueFree();
		}
	}
}
