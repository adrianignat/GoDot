using Godot;

[GlobalClass]
public partial class DamageUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Damage";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static DamageUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        DamageUpgradeResource resource = new DamageUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
