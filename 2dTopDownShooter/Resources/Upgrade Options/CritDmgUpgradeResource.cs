using Godot;
using System;

public partial class CritDmgUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Crit Dmg";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static CritDmgUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        CritDmgUpgradeResource resource = new CritDmgUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
