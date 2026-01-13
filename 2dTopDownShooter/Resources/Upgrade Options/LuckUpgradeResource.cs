using Godot;
using System;

public partial class LuckUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Luck";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static LuckUpgradeResource Create(UpgradeQuality quality, float luckBonus = 0f)
    {
        _rng.Randomize();

        LuckUpgradeResource resource = new LuckUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng, luckBonus)
        };

        return resource;
    }
}
