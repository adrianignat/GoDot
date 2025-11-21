using Godot;

namespace dTopDownShooter.Scripts.Characters
{
    public partial class Character : CharacterBody2D
    {
        private ushort health;
        [Export]
        internal float Speed;
        [Export]
        internal ushort Health
        {
            get
            {
                return health;
            }
            set
            {
                health = value;
                if (this is Player)
                    Game.Instance.EmitSignal(Game.SignalName.PlayerHealthChanged, health);
            }
        }

        internal bool IsDead => Health <= 0;

        internal void TakeDamage(ushort damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                OnKilled();
            }
            else
            {
                OnDamaged();
            }
        }

        internal virtual void OnDamaged()
        {
        }

        internal virtual void OnKilled()
        {
        }
    }
}
