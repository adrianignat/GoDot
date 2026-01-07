namespace dTopDownShooter.Scripts
{
	/// <summary>
	/// Centralized game constants for easy balancing and configuration.
	/// All gameplay-affecting values should be defined here for easy tweaking.
	/// </summary>
	public static class GameConstants
	{
		#region Player

		public const ushort PlayerInitialHealth = 100;            // HP
		public const float PlayerInitialSpeed = 200f;             // pixels/sec

		// Dash Ability
		public const float DashDistance = 150f;                   // pixels
		public const float DashDuration = 0.15f;                  // seconds
		public const float DashCooldown = 15f;                    // seconds

		#endregion

		#region Enemies - General

		/// <summary>
		/// Base health per enemy tier. Tier 1 = 100 HP, Tier 2 = 200 HP, etc.
		/// </summary>
		public const ushort EnemyBaseHealth = 100;                // HP per tier

		/// <summary>
		/// Y-distance threshold for determining attack direction (up/down/forward).
		/// </summary>
		public const short EnemyAttackDistanceDelta = 60;         // pixels

		#endregion

		#region Enemies - Torch Goblin (Melee)

		public const ushort TorchGoblinDamage = 10;               // HP per hit
		public const float TorchGoblinAttackSpeed = 1f;           // seconds between attacks
		public const float TorchGoblinMoveSpeed = 80f;            // pixels/sec

		#endregion

		#region Enemies - TNT Goblin (Ranged)

		public const float TntGoblinThrowRange = 150f;            // pixels - stops moving when this close
		public const float TntGoblinThrowCooldown = 1.5f;         // seconds between throws
		public const float TntGoblinThrowAnimationDelay = 0.25f;  // seconds - delay before dynamite spawns
		public const float TntGoblinMoveSpeed = 70f;              // pixels/sec

		#endregion

		#region Enemies - Shaman (Ranged Caster)

		public const float ShamanAttackCooldown = 2.0f;           // seconds between attacks
		public const float ShamanPreferredDistance = 200f;        // pixels - tries to maintain this distance
		public const float ShamanMoveSpeed = 60f;                 // pixels/sec
		public const int ShamanProjectileSpawnFrame = 7;          // 0-indexed animation frame
		public const ushort ShamanProjectileDamage = 10;          // HP per hit
		public const float ShamanProjectileSpeed = 300f;          // pixels/sec

		#endregion

		#region Enemy Spawning

		public const float InitialEnemySpawnRate = 0.5f;          // enemies/sec at game start
		public const float SpawnIncreaseRate = 1.1f;              // multiplier (1.1 = +10%)
		public const float SpawnRateIncreaseInterval = 30f;       // seconds between rate increases
		public const float TierIntroductionInterval = 60f;        // seconds between new tier unlocks
		public const float DayTransitionSpawnRetention = 0.7f;    // multiplier (0.7 = keep 70% of rate)
		public const float TntGoblinSpawnChance = 0.15f;          // 0-1 probability (0.15 = 15%)
		public const float ShamanSpawnChance = 0.10f;             // 0-1 probability (0.10 = 10%)
		public const float SpawnMargin = 50f;                     // pixels outside camera view
		public const int MaxSpawnAttempts = 100;                  // max retries to find valid location

		#endregion

		#region Weapons - Bow/Arrow

		public const float InitialArrowsPerSecond = 1.5f;         // arrows/sec
		public const ushort ArrowDamage = 100;                    // HP per hit
		public const float ArrowSpeed = 500f;                     // pixels/sec
		public const float ArrowLifespan = 3f;                    // seconds before despawn
		public const float ArrowBounceSpeedReduction = 0.3f;      // multiplier (0.3 = 30% speed retained)
		public const float ArrowPierceSpeedReduction = 0.7f;      // multiplier (0.7 = 70% speed retained)

		#endregion

		#region Weapons - Player Dynamite (Barrel)

		public const ushort PlayerDynamiteDamage = 100;           // HP per hit
		public const float PlayerDynamiteThrowDuration = 0.4f;    // seconds - flight time
		public const float PlayerDynamiteBlastRadius = 100f;      // pixels
		public const float PlayerDynamiteFuseTime = 1.0f;         // seconds after landing
		public const float PlayerDynamiteThrowRadius = 200f;      // pixels - random target range
		public const float PlayerDynamiteMaxThrowDistance = 150f; // pixels - max throw distance
		public const float InitialDynamiteThrowRate = 0.33f;      // throws/sec (~1 every 3 seconds)

		#endregion

		#region Weapons - Enemy Dynamite

		public const ushort EnemyDynamiteDamage = 10;             // HP per hit
		public const float EnemyDynamiteThrowDuration = 0.5f;     // seconds - flight time
		public const float EnemyDynamiteBlastRadius = 60f;        // pixels
		public const float EnemyDynamiteFuseTime = 1.0f;          // seconds after landing

		#endregion

		#region Day/Night Cycle

		public const float DayDuration = 180f;                    // seconds (3 minutes)
		public const float ShelterWarningTime = 30f;              // seconds before night starts
		public const float NightDuration = 60f;                   // seconds (1 minute)
		public const float NightDamageInterval = 3f;              // seconds between damage ticks
		public const ushort PlayerNightDamage = 15;               // HP per tick
		public const float ShelterRadius = 80f;                   // pixels from shelter center
		public const float BuildDetectionRadius = 130f;           // pixels - triggers construction
		public const float FadeDuration = 2.0f;                   // seconds - day transition fade

		#endregion

		#region Upgrades - Selection

		public const ushort FirstUpgradeThreshold = 5;            // gold required for first upgrade
		public const ushort UpgradeStepIncrement = 1;             // gold added to each threshold
		public const ushort LegendaryUpgradeChance = 5;           // percent (5 = 5%)
		public const ushort EpicUpgradeChance = 15;               // percent (15 = 15%)
		public const int UpgradeOptionCount = 3;                  // choices per upgrade screen

		#endregion

		#region Upgrades - Amounts by Rarity

		public const ushort UpgradeAmountCommon = 10;             // percent boost
		public const ushort UpgradeAmountRare = 20;               // percent boost
		public const ushort UpgradeAmountEpic = 30;               // percent boost
		public const ushort UpgradeAmountLegendary = 40;          // percent boost

		#endregion

		#region Upgrades - Max Values

		public const float MaxMagnetRadius = 300f;                // pixels - max pickup range
		public const float MaxDynamiteBlastRadius = 150f;         // pixels - max bonus radius

		#endregion

		#region Loot

		public const float GoldMagnetSpeed = 300f;                // pixels/sec toward player
		public const ushort GoldDropAmount = 1;                   // gold per enemy kill

		#endregion

		#region Map Generation

		public const int TileSize = 64;                           // pixels per tile
		public const int MapPieceSize = 1344;                     // pixels per map piece
		public const int MapGridSize = 4;                         // pieces per side (4x4 grid)
		public const int MapPieceVariants = 3;                    // variants per piece type
		public const int MapSpawnMarginTiles = 5;                 // tiles around playable area

		#endregion

		#region Collision Layers (bitmask values)

		public const uint MapCollisionLayer = 1;                  // Layer 1: terrain, buildings, trees
		public const uint WaterCollisionLayer = 128;              // Layer 7: water tiles

		#endregion

		#region Node Groups

		public const string EnemiesGroup = "enemies";
		public const string GoldGroup = "gold";
		public const string PlayerGroup = "player";
		public const string ArrowsGroup = "arrows";
		public const string DynamiteGroup = "dynamite";
		public const string ValidSpawnLocationGroup = "validSpawnLocation";

		#endregion
	}
}
