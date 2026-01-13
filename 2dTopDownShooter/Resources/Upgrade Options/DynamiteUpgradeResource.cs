using Godot;

public partial class DynamiteUpgradeResource : BaseUpgradeResource
{
	private static RandomNumberGenerator _rng = new();

	public override string UpgradeName => "Dynamite";

	// -------------------------------------------------
	// Factory method
	// -------------------------------------------------
	public static DynamiteUpgradeResource Create(UpgradeQuality quality, float luckBonus = 0f)
	{
		_rng.Randomize();

		DynamiteUpgradeResource resource = new DynamiteUpgradeResource
		{
			Quality = quality,
			PercentageIncrease = RollValue(quality, _rng, luckBonus)
		};

		return resource;
	}
}
