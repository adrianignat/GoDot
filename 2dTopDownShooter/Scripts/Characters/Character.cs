using Godot;

namespace dTopDownShooter.Scripts.Characters
{
    public partial class Character : CharacterBody2D
    {
        [Export]
        internal float Speed;
        [Export]
        internal short Health;

        internal bool IsDead => Health <= 0;

        [Signal]
        public delegate void KilledEventHandler();

        internal void TakeDamage(short damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                EmitSignal("Killed");
            }
        }
    }
}
