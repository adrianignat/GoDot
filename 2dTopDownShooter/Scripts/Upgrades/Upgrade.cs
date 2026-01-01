using Godot;

namespace dTopDownShooter.Scripts.Upgrades
{
	public partial class Upgrade : Sprite2D
	{
		internal UpgradeType Type { get; }
		internal RarityType Rarity { get; }
		internal ushort Amount { get; }

		internal string Title => Type switch
		{
			UpgradeType.Health => "Health",
			UpgradeType.WeaponSpeed => "Attack Speed",
			UpgradeType.Speed => "Movement",
			UpgradeType.Bouncing => "Bouncing",
			UpgradeType.Piercing => "Piercing",
			UpgradeType.Luck => "Luck",
			UpgradeType.Magnet => "Magnet",
			UpgradeType.Dynamite => "Dynamite",
			_ => string.Empty,
		};

		internal string Description => GetDescription();

		private string GetDescription()
		{
			var player = Game.Instance?.Player;
			var bow = player?.GetNodeOrNull<Bow>("Bow");
			var dynamiteThrower = player?.GetNodeOrNull<DynamiteThrower>("DynamiteThrower");

			return Type switch
			{
				UpgradeType.Health => $"+{Amount}% max health",
				UpgradeType.WeaponSpeed => $"+{Amount}% fire rate",
				UpgradeType.Speed => $"+{Amount}% move speed",
				UpgradeType.Bouncing => GetBouncingDescription(bow),
				UpgradeType.Piercing => GetPiercingDescription(bow),
				UpgradeType.Luck => $"+{Amount}% rare drop chance",
				UpgradeType.Magnet => $"+{Amount}% pickup range",
				UpgradeType.Dynamite => GetDynamiteDescription(dynamiteThrower),
				_ => string.Empty,
			};
		}

		private string GetBouncingDescription(Bow bow)
		{
			int currentLevel = bow?.BouncingLevel ?? 0;
			int newTotal = currentLevel + 1;
			if (currentLevel == 0)
				return $"Arrows ricochet to {newTotal} enemy";
			return $"Arrows ricochet to {newTotal} enemies";
		}

		private string GetPiercingDescription(Bow bow)
		{
			int currentLevel = bow?.PiercingLevel ?? 0;
			int newTotal = currentLevel + 1;
			if (currentLevel == 0)
				return $"Arrows pierce {newTotal} enemy";
			return $"Arrows pierce {newTotal} enemies";
		}

		private string GetDynamiteDescription(DynamiteThrower thrower)
		{
			bool isFirstUpgrade = thrower == null || thrower.ObjectsPerSecond == 0;
			if (isFirstUpgrade)
				return "Unlock explosive dynamite";
			return $"+{Amount}% blast radius";
		}

		internal Upgrade(UpgradeType type, RarityType rarity)
		{
			Type = type;
			Rarity = rarity;

			switch (rarity)
			{
				case RarityType.Common:
					Amount = 10;
					break;
				case RarityType.Rare:
					Amount = 20;
					break;
				case RarityType.Epic:
					Amount = 30;
					break;
				case RarityType.Legendary:
					Amount = 40;
					break;
			}
		}
	}

	public enum UpgradeType
	{
		Health = 0,
		WeaponSpeed = 1,
		Speed = 2,
		Bouncing = 3,
		Piercing = 4,
		Luck = 5,
		Magnet = 6,
		Dynamite = 7
	}

	internal enum RarityType
	{
		Common,
		Rare,
		Epic,
		Legendary
	}
}
