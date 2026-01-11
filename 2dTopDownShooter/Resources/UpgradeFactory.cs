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
    // -------------------------------------------------
    private static readonly List<Func<BaseUpgradeResource.UpgradeQuality, BaseUpgradeResource>>
        _registeredUpgrades = new()
        {
            quality => DamageUpgradeResource.Create(quality),
            quality => LuckUpgradeResource.Create(quality),
            quality => MoveSpeedUpgradeResource.Create(quality),
            quality => AtkSpeedUpgradeResource.Create(quality),
            quality => HealthUpgradeResource.Create(quality),
            quality => CritChanceUpgradeResource.Create(quality),
            quality => CritDmgUpgradeResource.Create(quality),
            quality => DodgeUpgradeResource.Create(quality),
        };

    // -------------------------------------------------
    // PUBLIC API — NO DUPLICATES
    // -------------------------------------------------
    public static List<BaseUpgradeResource> CreateUpgradeRoll(int count)
    {
        _rng.Randomize();

        List<BaseUpgradeResource> result = new();
        List<Func<BaseUpgradeResource.UpgradeQuality, BaseUpgradeResource>>
            available = new(_registeredUpgrades);

        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            var quality = RollQuality();

            int index = _rng.RandiRange(0, available.Count - 1);
            var creator = available[index];
            available.RemoveAt(index); // 🔑 prevents duplicates

            result.Add(creator.Invoke(quality));
        }

        return result;
    }

    // -------------------------------------------------
    // Quality roll (70 / 20 / 10)
    // -------------------------------------------------
    private static BaseUpgradeResource.UpgradeQuality RollQuality()
    {
        float roll = _rng.Randf();

        if (roll < 0.70f)
            return BaseUpgradeResource.UpgradeQuality.Common;

        if (roll < 0.90f)
            return BaseUpgradeResource.UpgradeQuality.Rare;

        return BaseUpgradeResource.UpgradeQuality.Epic;
    }
}
