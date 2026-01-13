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
	// Roll values by quality with luck bonus
	// -------------------------------------------------
	protected static float RollValue(
		UpgradeQuality quality,
		RandomNumberGenerator rng,
		float luckBonus = 0f
	)
	{
		float baseValue = quality switch
		{
			UpgradeQuality.Common => rng.RandfRange(1f, 3f),
			UpgradeQuality.Rare   => rng.RandfRange(3f, 6f),
			UpgradeQuality.Epic   => rng.RandfRange(6f, 10f),
			_ => 1f
		};

		// Apply luck bonus to the rolled value
		return baseValue + luckBonus;
	}
}
