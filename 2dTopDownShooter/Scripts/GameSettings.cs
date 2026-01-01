namespace dTopDownShooter.Scripts
{
	public enum ShootingStyle
	{
		AutoAim,    // Default: arrows home in on closest enemy
		Directional // Arrows shoot in the direction player is facing
	}

	/// <summary>
	/// User-configurable game settings.
	/// </summary>
	public static class GameSettings
	{
		public static ShootingStyle ShootingStyle { get; set; } = ShootingStyle.AutoAim;
	}
}
