using Godot;

namespace dTopDownShooter.Scripts.Upgrades
{
	public partial class Upgrade : Sprite2D
	{
		internal UpgradeType Type { get; }
		internal RarityType Rarity { get; }
		internal ushort Amount { get; }


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

	internal enum UpgradeType
	{
		Health = 0,
		WeaponSpeed = 1,
		Speed = 2
	}

	internal enum RarityType
	{
		Common,
		Rare,
		Epic,
		Legendary
	}
}
