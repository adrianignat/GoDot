using Godot;

namespace dTopDownShooter.Scripts.Characters
{
	internal partial class PlayerXPBar : ProgressBar
	{
		[Export] public ushort FirstUpgrade = 5;
		[Export] public ushort UpgradeStep = 1;

		private ushort _gold;
		private ushort _goldRequiredForNextUpgrade;
		private ushort _goldAtLastUpgrade;
		private ushort _currentUpgradeStep;

		public override void _Ready()
		{
			Game.Instance.GoldAcquired += OnGoldAcquired;
			Game.Instance.UpgradeSelected += OnUpgradeSelected;

			_currentUpgradeStep = FirstUpgrade;
			_goldRequiredForNextUpgrade = FirstUpgrade;
			_goldAtLastUpgrade = 0;

			UpdateBar();
		}

		private void OnGoldAcquired(ushort amount)
		{
			_gold += amount;
			UpdateBar();
		}

		private void OnUpgradeSelected(BaseUpgradeResource upgrade)
		{
			// After upgrade is selected, update thresholds
			_goldAtLastUpgrade = _goldRequiredForNextUpgrade;
			_currentUpgradeStep += UpgradeStep;
			_goldRequiredForNextUpgrade = (ushort)(_goldAtLastUpgrade + _currentUpgradeStep);

			UpdateBar();
		}

		private void UpdateBar()
		{
			// Calculate progress within current upgrade tier
			ushort goldInCurrentTier = (ushort)(_gold - _goldAtLastUpgrade);
			ushort goldNeededForTier = (ushort)(_goldRequiredForNextUpgrade - _goldAtLastUpgrade);

			MaxValue = goldNeededForTier;
			Value = Mathf.Min(goldInCurrentTier, goldNeededForTier);
		}
	}
}
