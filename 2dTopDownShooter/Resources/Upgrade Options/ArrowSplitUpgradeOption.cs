using Godot;
using System;

public partial class ArrowSplitUpgradeOption : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Arrow Split";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static ArrowSplitUpgradeOption Create(UpgradeQuality quality, float luckBonus = 0f)
    {
        _rng.Randomize();

        ArrowSplitUpgradeOption resource = new ArrowSplitUpgradeOption
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng, luckBonus)
        };

        return resource;
    }
}
