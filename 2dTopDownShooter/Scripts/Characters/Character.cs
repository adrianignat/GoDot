using Godot;

namespace dTopDownShooter.Scripts.Characters
{
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

        internal void TakeDamage(ushort damage)
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
            var tween = CreateTween();
            tween.TweenProperty(_animation, "modulate", new Color(10, 10, 10, 1), 0.05f);
            tween.TweenProperty(_animation, "modulate", new Color(1, 1, 1, 1), 0.1f);
        }
    }
}
