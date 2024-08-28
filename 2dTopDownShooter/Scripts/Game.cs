using Godot;

namespace dTopDownShooter.Scripts
{
	public partial class Game : Node
	{
		private static Game _gameInstance;

		internal bool IsPaused { get; set; }

		public static Game Instance 
		{ 
			get
			{
				_gameInstance ??= new Game();

				return _gameInstance;
			}
			private set
			{
			} 
		}

		[Signal]
		public delegate void EnemyKilledEventHandler(Enemy enemy);

		[Signal]
		public delegate void PlayerTakeDamageEventHandler(short damage);
	}
}
