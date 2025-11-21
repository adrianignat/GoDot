using Godot;

namespace dTopDownShooter.Scripts.Upgrades
{
	public partial class Upgrade : Sprite2D
	{
		internal UpgradeType Type { get; }
		internal RarityType Rarity { get; }
		internal ushort Amount { get; }
		internal string Description
		{
			get
			{
				return Type switch
				{
					UpgradeType.Health => $"Health",
					UpgradeType.WeaponSpeed => $"Weapon speed",
					UpgradeType.Speed => $"Movement speed",
					UpgradeType.Bouncing => $"Bouncing +1",
					UpgradeType.Piercing => $"Piercing +1",
					_ => string.Empty,
				};
			}
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
		Piercing = 4
	}

	internal enum RarityType
	{
		Common,
		Rare,
		Epic,
		Legendary
	}
}
