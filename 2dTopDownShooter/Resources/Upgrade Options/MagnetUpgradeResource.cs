using Godot;

public partial class MagnetUpgradeResource : BaseUpgradeResource
{
	private static RandomNumberGenerator _rng = new();

	public override string UpgradeName => "Magnet";

	// -------------------------------------------------
	// Factory method
	// -------------------------------------------------
	public static MagnetUpgradeResource Create(UpgradeQuality quality)
	{
		_rng.Randomize();

		MagnetUpgradeResource resource = new MagnetUpgradeResource
		{
			Quality = quality,
			PercentageIncrease = RollValue(quality, _rng)
		};

		return resource;
	}
}
