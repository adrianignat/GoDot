using Godot;

namespace dTopDownShooter.Scripts.Characters
{
    /// <summary>
    /// Base class for all characters (Player and Enemies).
    /// Provides health management, damage handling, and hit flash animation.
    /// </summary>
    public partial class Character : CharacterBody2D
    {
        internal AnimatedSprite2D _animation;
        private ushort _health;

        [Export]
        internal float Speed;
        [Export]
        internal ushort Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
                if (this is Player)
                    Game.Instance.EmitSignal(Game.SignalName.PlayerHealthChanged, _health);
            }
        }

        internal bool IsDead => Health <= 0;

        /// <summary>
        /// Applies damage to the character, handling underflow protection.
        /// Triggers <see cref="OnDamaged"/> or <see cref="OnKilled"/> as appropriate.
        /// </summary>
        internal virtual void TakeDamage(ushort damage)
        {
            // Check before subtracting to prevent unsigned underflow
            if (damage >= Health)
            {
                Health = 0;
                OnKilled();
            }
            else
            {
                Health -= damage;
                OnDamaged();
            }
        }

        internal virtual void OnDamaged()
        {
            PlayHitFlash();
        }

        internal virtual void OnKilled()
        {
        }

        private void PlayHitFlash()
        {
            // Brief white flash to indicate damage
            // Using bright red-tinted flash for better visibility
            var tween = CreateTween();
            tween.TweenProperty(_animation, "modulate", new Color(1, 0.3f, 0.3f, 1), 0.05f);
            tween.TweenProperty(_animation, "modulate", Colors.White, 0.1f);
        }
    }
}
