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
    // Registered upgrade creators (ONE PER TYPE)
    // Takes quality and luck bonus percentage
    // -------------------------------------------------
    private static readonly List<Func<BaseUpgradeResource.UpgradeQuality, float, BaseUpgradeResource>>
        _registeredUpgradesWithLuck = new()
        {
            (quality, luckBonus) => DamageUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => LuckUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => MoveSpeedUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => AtkSpeedUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => HealthUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => CritChanceUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => CritDmgUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => DodgeUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => FreezeChanceUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => BurnChanceUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => ArrowSplitUpgradeOption.Create(quality, luckBonus),
            (quality, luckBonus) => MagnetUpgradeResource.Create(quality, luckBonus),
            (quality, luckBonus) => DynamiteUpgradeResource.Create(quality, luckBonus),
        };

    // -------------------------------------------------
    // PUBLIC API — NO DUPLICATES
    // -------------------------------------------------
    public static List<BaseUpgradeResource> CreateUpgradeRoll(int count, int luckLevel = 0)
    {
        _rng.Randomize();

        List<BaseUpgradeResource> result = new();
        List<Func<BaseUpgradeResource.UpgradeQuality, float, BaseUpgradeResource>>
            available = new(_registeredUpgradesWithLuck);

        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            var quality = RollQuality(luckLevel);

            int index = _rng.RandiRange(0, available.Count - 1);
            var creator = available[index];
            available.RemoveAt(index); // 🔑 prevents duplicates

            // Pass luck bonus percentage to affect roll amounts
            float luckBonus = luckLevel * LuckBonusPerLevel;
            result.Add(creator.Invoke(quality, luckBonus));
        }

        return result;
    }

    // Luck bonus per level (e.g., 0.5 = each luck level adds 0.5% to rolls)
    private const float LuckBonusPerLevel = 0.5f;

    // Luck bonus to quality chances per level
    private const float LuckQualityBonusPerLevel = 1f; // 1% per luck level

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
