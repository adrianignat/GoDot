using Godot;
using System;

public partial class AtkSpeedUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Atk Speed";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static AtkSpeedUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        AtkSpeedUpgradeResource resource = new AtkSpeedUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
