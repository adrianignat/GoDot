using Godot;

namespace dTopDownShooter.Scripts.Characters
{
	internal partial class PlayerHealthBar : ProgressBar
	{
		ProgressBar damageBar;
		short health = 100;

		public override void _Ready()
		{
			//var timer = GetNode<Timer>("Timer");
			//damageBar = GetNode<ProgressBar>("DamageBar");
			Game.Instance.PlayerTakeDamage += OnPlayerTakeDamage;
			//TODO: this is needed only on delayed heal
			//timer.Timeout += () => damageBar.Value = health;
			//damageBar.MaxValue = health;
			//damageBar.Value = health;
			Value = 100;
			MaxValue = 100;
		}

		private void OnPlayerTakeDamage(short damage)
		{
			//health -= damage;
			Value -= damage;
		}
	}
}
