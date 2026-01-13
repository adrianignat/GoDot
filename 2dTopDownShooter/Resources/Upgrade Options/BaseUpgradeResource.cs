using Godot;

public abstract partial class BaseUpgradeResource : Resource
{
	// -------------------------------------------------
	// Upgrade quality
	// -------------------------------------------------
	public enum UpgradeQuality
	{
		Common,
		Rare,
		Epic
	}

	// -------------------------------------------------
	// Shared upgrade data
	// -------------------------------------------------
	[Export] public UpgradeQuality Quality;
	[Export] public float PercentageIncrease;

	// -------------------------------------------------
	// Display name (implemented per upgrade)
	// -------------------------------------------------
	public abstract string UpgradeName { get; }

	// -------------------------------------------------
	// Display description (can be overridden)
	// -------------------------------------------------
	public virtual string DisplayDescription =>
		$"+{PercentageIncrease:F1}% {UpgradeName}";

	// -------------------------------------------------
	// Roll values by quality
	// -------------------------------------------------
	protected static float RollValue(
		UpgradeQuality quality,
		RandomNumberGenerator rng
	)
	{
		return quality switch
		{
			UpgradeQuality.Common => rng.RandfRange(5f, 10f),
			UpgradeQuality.Rare   => rng.RandfRange(10f, 20f),
			UpgradeQuality.Epic   => rng.RandfRange(20f, 35f),
			_ => 5f
		};
	}
}
