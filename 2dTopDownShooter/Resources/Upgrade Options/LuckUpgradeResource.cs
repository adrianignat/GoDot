using Godot;

public partial class LuckUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Luck";

    // -------------------------------------------------
    // Factory method - Luck uses lower values than other upgrades
    // -------------------------------------------------
    public static LuckUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        // Luck uses lower percentage ranges to prevent being overpowered
        float value = quality switch
        {
            UpgradeQuality.Common => _rng.RandfRange(1f, 2f),
            UpgradeQuality.Rare   => _rng.RandfRange(2f, 4f),
            UpgradeQuality.Epic   => _rng.RandfRange(4f, 6f),
            _ => 1f
        };

        LuckUpgradeResource resource = new LuckUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = value
        };

        return resource;
    }
}
