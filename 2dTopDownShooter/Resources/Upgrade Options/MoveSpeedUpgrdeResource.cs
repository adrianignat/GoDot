using Godot;

[GlobalClass]
public partial class MoveSpeedUpgradeResource : BaseUpgradeResource
{
    private static RandomNumberGenerator _rng = new();

    public override string UpgradeName => "Move Speed";

    // -------------------------------------------------
    // Factory method
    // -------------------------------------------------
    public static MoveSpeedUpgradeResource Create(UpgradeQuality quality)
    {
        _rng.Randomize();

        MoveSpeedUpgradeResource resource = new MoveSpeedUpgradeResource
        {
            Quality = quality,
            PercentageIncrease = RollValue(quality, _rng)
        };

        return resource;
    }
}
