using dTopDownShooter.Scripts;
using Godot;
using System;
using System.Collections.Generic;

public static class UpgradeFactory
{
    // -------------------------------------------------
    // RNG
    // -------------------------------------------------
    private static readonly RandomNumberGenerator _rng = new();

    // -------------------------------------------------
    // Upgrade creator with type for cap checking
    // -------------------------------------------------
    private record UpgradeCreator(
        Type UpgradeType,
        Func<BaseUpgradeResource.UpgradeQuality, BaseUpgradeResource> Create
    );

    // -------------------------------------------------
    // Registered upgrade creators (ONE PER TYPE)
    // -------------------------------------------------
    private static readonly List<UpgradeCreator> _registeredUpgrades = new()
    {
        new(typeof(DamageUpgradeResource), quality => DamageUpgradeResource.Create(quality)),
        new(typeof(LuckUpgradeResource), quality => LuckUpgradeResource.Create(quality)),
        new(typeof(MoveSpeedUpgradeResource), quality => MoveSpeedUpgradeResource.Create(quality)),
        new(typeof(AtkSpeedUpgradeResource), quality => AtkSpeedUpgradeResource.Create(quality)),
        new(typeof(HealthUpgradeResource), quality => HealthUpgradeResource.Create(quality)),
        new(typeof(CritChanceUpgradeResource), quality => CritChanceUpgradeResource.Create(quality)),
        new(typeof(CritDmgUpgradeResource), quality => CritDmgUpgradeResource.Create(quality)),
        new(typeof(DodgeUpgradeResource), quality => DodgeUpgradeResource.Create(quality)),
        new(typeof(FreezeChanceUpgradeResource), quality => FreezeChanceUpgradeResource.Create(quality)),
        new(typeof(BurnChanceUpgradeResource), quality => BurnChanceUpgradeResource.Create(quality)),
        new(typeof(ArrowSplitUpgradeOption), quality => ArrowSplitUpgradeOption.Create(quality)),
        new(typeof(MagnetUpgradeResource), quality => MagnetUpgradeResource.Create(quality)),
        new(typeof(DynamiteUpgradeResource), quality => DynamiteUpgradeResource.Create(quality)),
        new(typeof(PierceChanceUpgradeResource), quality => PierceChanceUpgradeResource.Create(quality)),
    };

    // -------------------------------------------------
    // PUBLIC API — NO DUPLICATES, FILTERS CAPPED UPGRADES
    // -------------------------------------------------
    public static List<BaseUpgradeResource> CreateUpgradeRoll(int count, int luckLevel = 0)
    {
        _rng.Randomize();

        var player = Game.Instance.Player;
        var bow = player.GetNode<Bow>("Bow");
        var dynamiteThrower = player.GetDynamiteThrower();
        var magnetShape = player.GetMagnetShape();

        // Filter out capped upgrades
        List<UpgradeCreator> available = new();
        foreach (var creator in _registeredUpgrades)
        {
            if (!IsUpgradeCapped(creator.UpgradeType, player, bow, dynamiteThrower, magnetShape))
            {
                available.Add(creator);
            }
        }

        List<BaseUpgradeResource> result = new();
        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            var quality = RollQuality(luckLevel);

            int index = _rng.RandiRange(0, available.Count - 1);
            var creator = available[index];
            available.RemoveAt(index); // 🔑 prevents duplicates

            result.Add(creator.Create(quality));
        }

        return result;
    }

    // -------------------------------------------------
    // Check if an upgrade type has reached its cap
    // -------------------------------------------------
    private static bool IsUpgradeCapped(
        Type upgradeType,
        Player player,
        Bow bow,
        DynamiteThrower dynamiteThrower,
        CircleShape2D magnetShape)
    {
        if (upgradeType == typeof(DodgeUpgradeResource))
            return player.DodgeChance >= 50f;

        if (upgradeType == typeof(MagnetUpgradeResource))
            return magnetShape.Radius >= GameConstants.MaxMagnetRadius;

        if (upgradeType == typeof(DynamiteUpgradeResource))
            return dynamiteThrower.BonusBlastRadius >=
                   GameConstants.MaxDynamiteBlastRadius - GameConstants.PlayerDynamiteBlastRadius;

        return false;
    }

    // Luck bonus to quality chances per level (0.5% per luck level)
    private const float LuckQualityBonusPerLevel = 0.5f;

    // -------------------------------------------------
    // Quality roll with luck bonus
    // Base: 70% Common / 20% Rare / 10% Epic
    // Each luck level adds 1% to Rare and Epic chances
    // -------------------------------------------------
    private static BaseUpgradeResource.UpgradeQuality RollQuality(int luckLevel)
    {
        float roll = _rng.Randf() * 100f;

        // Calculate adjusted chances
        float epicChance = 10f + luckLevel * LuckQualityBonusPerLevel;
        float rareChance = 20f + luckLevel * LuckQualityBonusPerLevel;

        // Cap at reasonable values
        epicChance = Mathf.Min(epicChance, 40f);
        rareChance = Mathf.Min(rareChance, 50f);

        if (roll < epicChance)
            return BaseUpgradeResource.UpgradeQuality.Epic;

        if (roll < epicChance + rareChance)
            return BaseUpgradeResource.UpgradeQuality.Rare;

        return BaseUpgradeResource.UpgradeQuality.Common;
    }
}
