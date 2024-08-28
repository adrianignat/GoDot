using Godot;
using System.Xml.Linq;

namespace dTopDownShooter.Scripts.Characters
{
    public partial class Character : CharacterBody2D
    {
        [Export]
        internal float Speed;
        [Export]
        internal short Health;

        internal bool IsDead => Health <= 0;

        internal void TakeDamage(short damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                OnKilled();
            }
        }

        internal virtual void OnKilled()
        {
            var game = Game.Instance;
            game.EmitSignal(Game.SignalName.EnemyKilled, this);
        }
    }
}
