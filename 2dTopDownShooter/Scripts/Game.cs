using dTopDownShooter.Scripts.Upgrades;
using Godot;


namespace dTopDownShooter.Scripts
{
	public partial class Game : Node2D
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

		public Window MainWindow { get; set; }

		public Player Player { get; set; }

		public override void _Ready()
		{
			Instance.MainWindow = GetTree().Root;
			Instance.Player = GetTree().Root.GetNode("main").GetNode<Player>("Player");
		}

		[Signal]
		public delegate void EnemyKilledEventHandler(Enemy enemy);

		[Signal]
		public delegate void PlayerTakeDamageEventHandler(ushort damage);

		[Signal]
		public delegate void PlayerHealthChangedEventHandler(ushort health);

		[Signal]
		public delegate void GoldAcquiredEventHandler(ushort amount);

		[Signal]
		public delegate void UpgradeReadyEventHandler();

		[Signal]
		public delegate void UpgradeSelectedEventHandler(Upgrade upgdade);
	}
}
