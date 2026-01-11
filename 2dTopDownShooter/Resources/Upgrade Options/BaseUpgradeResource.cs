using Godot;

public abstract partial class BaseUpgradeResource : Resource
{
    // -------------------------------------------------
    // Upgrade quality
    // -------------------------------------------------
    public enum UpgradeQuality
    {
        Common,
        Rare,
        Epic
    }

    // -------------------------------------------------
    // Shared upgrade data
    // -------------------------------------------------
    [Export] public UpgradeQuality Quality;
    [Export] public float PercentageIncrease;

    // -------------------------------------------------
    // Display name (implemented per upgrade)
    // -------------------------------------------------
    public abstract string UpgradeName { get; }

    // -------------------------------------------------
    // Roll values by quality
    // -------------------------------------------------
    protected static float RollValue(
        UpgradeQuality quality,
        RandomNumberGenerator rng
    )
    {
        return quality switch
        {
            UpgradeQuality.Common => rng.RandfRange(1f, 3f),
            UpgradeQuality.Rare   => rng.RandfRange(3f, 6f),
            UpgradeQuality.Epic   => rng.RandfRange(6f, 10f),
            _ => 1f
        };
    }
}
