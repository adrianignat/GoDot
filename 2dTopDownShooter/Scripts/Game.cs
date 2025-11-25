using dTopDownShooter.Scripts.Upgrades;
using Godot;


namespace dTopDownShooter.Scripts
{
	public partial class Game : Node2D
	{
		private static Game _gameInstance;

		internal bool IsPaused { get; set; }

		internal bool ShouldRestart { get; set; } = false;

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

		public Node2D MainWindow { get; set; }

		public Player Player { get; set; }

		public override void _Ready()
		{
			Instance.MainWindow = GetTree().Root.GetNode<Node2D>("main");
			Instance.Player = Instance.MainWindow.GetNode<Player>("Player");

			// Position player at map center if MapGenerator exists
			var mapGenerator = Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			if (mapGenerator != null)
			{
				Instance.Player.Position = mapGenerator.GetPlayerSpawnPosition();

				// Update camera limits based on map size
				var camera = Instance.Player.GetNodeOrNull<Camera2D>("Camera2D");
				if (camera != null)
				{
					var mapSize = mapGenerator.GetMapSize();
					// Camera limits define the edges of what can be shown
					camera.LimitLeft = 0;
					camera.LimitTop = 0;
					camera.LimitRight = (int)mapSize.X;
					camera.LimitBottom = (int)mapSize.Y;
				}
			}
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			if (Instance.ShouldRestart)
			{
				Restart();
				ShouldRestart = false;
			}
		}

		internal void Restart()
		{
			//var enemies = GetTree().GetNodesInGroup("enemies");
			//foreach (Enemy enemy in enemies)
			//{
			//	enemy.QueueFree();
			//}
			//Instance.Player = new Player();
			Instance.MainWindow.GetTree().ReloadCurrentScene();
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
