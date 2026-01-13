using Godot;
using System;

public partial class BurnChanceUpgradeResource : BaseUpgradeResource
{
	private static RandomNumberGenerator _rng = new();

	public override string UpgradeName => "Arrow Burn";

	// -------------------------------------------------
	// Factory method
	// -------------------------------------------------
	public static BurnChanceUpgradeResource Create(UpgradeQuality quality, float luckBonus = 0f)
	{
		_rng.Randomize();

		BurnChanceUpgradeResource resource = new BurnChanceUpgradeResource
		{
			Quality = quality,
			PercentageIncrease = RollValue(quality, _rng, luckBonus)
		};

		return resource;
	}
}
