using Godot;

public partial class DynamiteUpgradeResource : BaseUpgradeResource
{
	private static RandomNumberGenerator _rng = new();

	public override string UpgradeName => "Dynamite";

	// -------------------------------------------------
	// Factory method
	// -------------------------------------------------
	public static DynamiteUpgradeResource Create(UpgradeQuality quality)
	{
		_rng.Randomize();

		DynamiteUpgradeResource resource = new DynamiteUpgradeResource
		{
			Quality = quality,
			PercentageIncrease = RollValue(quality, _rng)
		};

		return resource;
	}
}
