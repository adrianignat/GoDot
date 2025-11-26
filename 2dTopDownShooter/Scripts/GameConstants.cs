namespace dTopDownShooter.Scripts
{
	/// <summary>
	/// Centralized game constants for easy balancing and configuration.
	/// </summary>
	public static class GameConstants
	{
		// Day/Night Cycle
		public const float DayDuration = 180f;           // 3 minutes
		public const float ShelterWarningTime = 30f;     // Warning starts 30s before night
		public const float NightDuration = 60f;          // 1 minute
		public const float NightDamageInterval = 3f;     // Damage every 3 seconds
		public const float ShelterRadius = 80f;          // Distance to count as "in shelter"
		public const float FadeDuration = 1.0f;          // Day transition fade duration

		// Combat
		public const ushort PlayerNightDamage = 15;      // Damage taken per tick at night
		public const ushort EnemyBaseHealth = 100;       // Health per enemy tier

		// Upgrades
		public const float MaxMagnetRadius = 300f;
		public const float MaxDynamiteBlastRadius = 150f;

		// Enemy AI
		public const short EnemyAttackDistanceDelta = 60; // Y-distance threshold for attack direction

		// Spawning
		public const float SpawnMargin = 50f;            // Distance outside camera to spawn enemies
		public const int MaxSpawnAttempts = 100;         // Max tries to find valid spawn location

		// Node Groups
		public const string EnemiesGroup = "enemies";
		public const string GoldGroup = "gold";
		public const string PlayerGroup = "player";
		public const string ValidSpawnLocationGroup = "validSpawnLocation";
	}
}
