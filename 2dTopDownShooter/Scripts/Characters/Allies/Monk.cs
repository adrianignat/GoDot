using Godot;

namespace dTopDownShooter.Scripts.Characters.Allies
{
	public enum MonkState
	{
		Idle,
		Running,
		Healing
	}

	/// <summary>
	/// Monk NPC that follows the player and periodically heals them.
	/// </summary>
	public partial class Monk : Character
	{
		private const float PathUpdateInterval = 0.25f;
		private const float StopDistanceBuffer = 20f; // Hysteresis buffer to prevent flickering

		private Player _player;
		private NavigationAgent2D _navigationAgent;
		private float _healTimer;
		private float _pathUpdateTimer;
		private bool _isFollowing = false;
		private bool _isMovingToPlayer = false; // Track if we're actively moving
		private MonkState _currentState = MonkState.Idle;

		public override void _Ready()
		{
			_player = Game.Instance.Player;
			_animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
			_navigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");

			// Initialize from constants
			Speed = GameConstants.MonkMoveSpeed;
			Health = GameConstants.MonkHealth;

			// Add to allies group for cleanup
			AddToGroup(GameConstants.AlliesGroup);

			// Connect animation finished signal
			_animation.AnimationFinished += OnAnimationFinished;

			// Start idle
			SetState(MonkState.Idle);

			// Randomize initial path update
			_pathUpdateTimer = (float)GD.RandRange(0.0, PathUpdateInterval);
			_healTimer = GameConstants.MonkHealInterval;

			GD.Print("[Monk] Spawned and ready");
		}

		private void SetState(MonkState newState)
		{
			if (_currentState == newState)
				return;

			_currentState = newState;

			switch (newState)
			{
				case MonkState.Idle:
					_animation.Play("idle");
					break;
				case MonkState.Running:
					_animation.Play("run");
					break;
				case MonkState.Healing:
					_animation.Play("heal");
					break;
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			if (_player == null || !IsInstanceValid(_player))
				return;

			// Update heal timer (always, even when not following)
			UpdateHealing(delta);

			// Skip movement while healing
			if (_currentState == MonkState.Healing)
				return;

			// Only move if following
			if (!_isFollowing)
				return;

			float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

			// Hysteresis: stop when close, only start moving again when far enough
			// This prevents flickering at the boundary
			if (_isMovingToPlayer)
			{
				// Currently moving - stop when we get close enough
				if (distanceToPlayer <= GameConstants.MonkFollowDistance)
				{
					_isMovingToPlayer = false;
					Velocity = Vector2.Zero;
					SetState(MonkState.Idle);
					return;
				}
			}
			else
			{
				// Currently idle - only start moving if player is far enough away
				if (distanceToPlayer <= GameConstants.MonkFollowDistance + StopDistanceBuffer)
				{
					// Stay idle
					Velocity = Vector2.Zero;
					SetState(MonkState.Idle);
					return;
				}
				// Player moved far enough, start following
				_isMovingToPlayer = true;
			}

			// Update path periodically
			_pathUpdateTimer -= (float)delta;
			if (_pathUpdateTimer <= 0)
			{
				_pathUpdateTimer = PathUpdateInterval;
				_navigationAgent.TargetPosition = _player.GlobalPosition;
			}

			// Move toward player
			if (!_navigationAgent.IsNavigationFinished())
			{
				Vector2 nextPosition = _navigationAgent.GetNextPathPosition();
				Vector2 direction = (nextPosition - GlobalPosition).Normalized();
				Velocity = direction * Speed;

				// Update animation
				SetState(MonkState.Running);
				_animation.FlipH = direction.X < 0;

				MoveAndSlide();
			}
			else
			{
				// Navigation finished but still too far - force path update
				Velocity = Vector2.Zero;
				_pathUpdateTimer = 0;
			}
		}

		private void UpdateHealing(double delta)
		{
			// Don't start new heal while already healing
			if (_currentState == MonkState.Healing)
				return;

			_healTimer -= (float)delta;

			if (_healTimer <= 0)
			{
				_healTimer = GameConstants.MonkHealInterval;
				TryHeal();
			}
		}

		private void TryHeal()
		{
			if (_player == null || !IsInstanceValid(_player))
				return;

			// Only heal if player needs it and monk is close enough
			float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
			if (distance > GameConstants.MonkFollowDistance * 2.5f)
				return;

			// Check if player needs healing (not at max health)
			if (_player.Health >= _player.MaxHealth)
				return;

			// Start heal animation
			SetState(MonkState.Healing);
			GD.Print("[Monk] Starting heal animation...");
		}

		private void OnAnimationFinished()
		{
			// Only handle heal animation finish
			if (_currentState == MonkState.Healing)
			{
				// Perform the heal
				PerformHeal();

				// Return to appropriate state
				if (_isFollowing && _player != null && IsInstanceValid(_player))
				{
					float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
					if (distance <= GameConstants.MonkFollowDistance)
						SetState(MonkState.Idle);
					else
						SetState(MonkState.Running);
				}
				else
				{
					SetState(MonkState.Idle);
				}
			}
		}

		private void PerformHeal()
		{
			if (_player == null || !IsInstanceValid(_player))
				return;

			// Emit the signal - let the player handle the actual healing
			Game.Instance.EmitSignal(Game.SignalName.MonkHealed, GameConstants.MonkHealAmount);
			GD.Print($"[Monk] Emitted heal signal for {GameConstants.MonkHealAmount} HP");
		}

		/// <summary>
		/// Start following the player.
		/// </summary>
		public void StartFollowing()
		{
			_isFollowing = true;
			_isMovingToPlayer = true; // Start moving immediately

			// Only start running if not currently healing
			if (_currentState != MonkState.Healing)
				SetState(MonkState.Running);

			GD.Print("[Monk] Now following player");
		}

		/// <summary>
		/// Stop following the player.
		/// </summary>
		public void StopFollowing()
		{
			_isFollowing = false;
			_isMovingToPlayer = false;
			Velocity = Vector2.Zero;

			// Only change to idle if not currently healing
			if (_currentState != MonkState.Healing)
				SetState(MonkState.Idle);

			GD.Print("[Monk] Stopped following");
		}

		internal override void OnKilled()
		{
			GD.Print("[Monk] Monk has been killed!");
			QueueFree();
		}

		public override void _ExitTree()
		{
			// Disconnect animation signal when removed
			if (_animation != null)
				_animation.AnimationFinished -= OnAnimationFinished;
		}
	}
}
