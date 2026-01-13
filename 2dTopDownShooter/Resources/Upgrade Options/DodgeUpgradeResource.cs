using Godot;

public partial class DodgeUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Dodge";

    // -------------------------------------------------
    // Factory method - Dodge uses lower values (capped at 50% total)
    // -------------------------------------------------
    public static DodgeUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        // Dodge uses lower percentage ranges to prevent being overpowered
        float value = quality switch
        {
            UpgradeQuality.Common => _rng.RandfRange(1f, 3f),
            UpgradeQuality.Rare   => _rng.RandfRange(3f, 5f),
            UpgradeQuality.Epic   => _rng.RandfRange(5f, 8f),
            _ => 1f
        };

        DodgeUpgradeResource resource = new DodgeUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = value
        };

        return resource;
    }
}
