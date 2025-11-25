using Godot;

namespace dTopDownShooter.Scripts.Characters
{
	internal partial class PlayerHealthBar : ProgressBar
	{
		ProgressBar damageBar;

		public override void _Ready()
		{
			//var timer = GetNode<Timer>("Timer");
			//damageBar = GetNode<ProgressBar>("DamageBar");
			Game.Instance.PlayerHealthChanged += PlayerHealthChanged;
			//TODO: this is needed only on delayed heal
			//timer.Timeout += () => damageBar.Value = health;
			//damageBar.MaxValue = health;
			//damageBar.Value = health;
			Value = 100;
			MaxValue = 100;
			ResizeHealthBar();

		}

		private void PlayerHealthChanged(ushort health)
		{	
			if (health > MaxValue)
			{
				MaxValue = health;
			}

			Value = health;
			ResizeHealthBar();

		}

		private void ResizeHealthBar()
		{
			// Cap the visual width to prevent it from going off screen
			const float maxWidth = 500f;
			float newWidth = Mathf.Min((float)MaxValue * 5f, maxWidth);
			Size = new Vector2(newWidth, Size.Y);
		}
	}
}
