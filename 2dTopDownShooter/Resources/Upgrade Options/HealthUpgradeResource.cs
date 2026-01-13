using Godot;
using System;

public partial class HealthUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Health";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static HealthUpgradeResource Create(UpgradeQuality quality, float luckBonus = 0f)
    {
        _rng.Randomize();

        HealthUpgradeResource resource = new HealthUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng, luckBonus)
        };

        return resource;
    }
}
