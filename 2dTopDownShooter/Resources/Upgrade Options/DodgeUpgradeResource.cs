using Godot;
using System;

public partial class DodgeUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Dodge";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static DodgeUpgradeResource Create(UpgradeQuality quality, float luckBonus = 0f)
    {
        _rng.Randomize();

        DodgeUpgradeResource resource = new DodgeUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng, luckBonus)
        };

        return resource;
    }
}
