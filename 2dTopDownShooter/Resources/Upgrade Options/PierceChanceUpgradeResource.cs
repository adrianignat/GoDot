using Godot;
using System;

public partial class PierceChanceUpgradeResource : BaseUpgradeResource
{
	public override string UpgradeName => "Arrow Pierce";

	public override string DisplayDescription =>
		$"+{PierceAmount} enemies pierced";

	/// <summary>
	/// The number of additional enemies arrows can pierce through.
	/// </summary>
	public int PierceAmount { get; private set; }

	// -------------------------------------------------
	// Factory method - flat +1/+2/+3 based on quality
	// -------------------------------------------------
	public static PierceChanceUpgradeResource Create(UpgradeQuality quality)
	{
		int pierceAmount = quality switch
		{
			UpgradeQuality.Common => 1,
			UpgradeQuality.Rare => 2,
			UpgradeQuality.Epic => 3,
			_ => 1
		};

		PierceChanceUpgradeResource resource = new PierceChanceUpgradeResource
		{
			Quality = quality,
			PierceAmount = pierceAmount,
			PercentageIncrease = pierceAmount // For display purposes
		};

		return resource;
	}
}
