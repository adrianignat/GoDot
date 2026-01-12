using Godot;
using System;

public partial class PierceChanceUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Arrow Pierce";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static PierceChanceUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        PierceChanceUpgradeResource resource = new PierceChanceUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
