using Godot;
using System;

public partial class FreezeChanceUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Arrow Freeze";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static FreezeChanceUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        FreezeChanceUpgradeResource resource = new FreezeChanceUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
