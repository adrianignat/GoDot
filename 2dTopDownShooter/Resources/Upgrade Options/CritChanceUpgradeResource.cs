using Godot;
using System;

public partial class CritChanceUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Crit Chance";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static CritChanceUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        CritChanceUpgradeResource resource = new CritChanceUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
