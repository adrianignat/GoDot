using dTopDownShooter.Scripts.UI;
using dTopDownShooter.Scripts.Upgrades;
using Godot;


namespace dTopDownShooter.Scripts
{
	public enum GamePhase
	{
		Day,
		ShelterWarning,  // Last 30 seconds of day
		Night
	}

	public partial class Game : Node2D
	{
		private static Game _gameInstance;

		internal bool IsPaused { get; set; }

		internal bool ShouldRestart { get; set; } = false;

		// Day/Night cycle state
		public int CurrentDay { get; set; } = 1;
		public GamePhase CurrentPhase { get; set; } = GamePhase.Day;
		public bool IsInShelter { get; set; } = false;

		// Game modes
		public bool WifeMode { get; set; } = false;

		// Day/Night components
		public DayNightManager DayNightManager { get; private set; }
		private DayNightUI _dayNightUI;
		private ShelterMarker _shelterMarker;

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

		public override void _EnterTree()
		{
			// Set singleton BEFORE any child _Ready() methods run
			// This ensures Game.Instance returns this node when children connect to signals
			_gameInstance = this;
		}

		public override void _Ready()
		{
			MainWindow = GetTree().Root.GetNode<Node2D>("main");
			Player = MainWindow.GetNode<Player>("Player");

			// Position player at map center if MapGenerator exists
			var mapGenerator = MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			if (mapGenerator != null)
			{
				Player.Position = mapGenerator.GetPlayerSpawnPosition();

				// Update camera limits based on map size
				var camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
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

			// Initialize Day/Night system
			InitializeDayNightSystem();
		}

		private void InitializeDayNightSystem()
		{
			// Create DayNightManager
			DayNightManager = new DayNightManager();
			DayNightManager.Name = "DayNightManager";
			AddChild(DayNightManager);

			// Create DayNightUI
			_dayNightUI = new DayNightUI();
			_dayNightUI.Name = "DayNightUI";
			_dayNightUI.Layer = 50; // Above most things but below fade overlay
			AddChild(_dayNightUI);
			_dayNightUI.SetDayNightManager(DayNightManager);

			// Create ShelterMarker
			_shelterMarker = new ShelterMarker();
			_shelterMarker.Name = "ShelterMarker";
			AddChild(_shelterMarker);
			_shelterMarker.SetDayNightManager(DayNightManager);
		}

		public void ApplyGameMode()
		{
			if (WifeMode)
			{
				// Wife Mode bonuses
				Player.Health = 200;      // Double health (normally 100)
				Player.Speed = 225;       // 50% more speed (normally 150)

				var bow = Player.GetNode<Bow>("Bow");
				bow.ObjectsPerSecond = 2.0f;  // Double fire rate (normally 1.0)

				GD.Print("Wife Mode activated! Bonuses applied.");
			}
		}

		public void StartFirstDay()
		{
			DayNightManager?.StartDay(1);
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			if (ShouldRestart)
			{
				Restart();
				ShouldRestart = false;
			}
		}

		internal void Restart()
		{
			// Clean up all spawned entities before reload
			CleanupAllEntities();

			// Reload the scene (regenerates map)
			MainWindow.GetTree().ReloadCurrentScene();
		}

		private void CleanupAllEntities()
		{
			// Remove all enemies
			foreach (Node node in GetTree().GetNodesInGroup(GameConstants.EnemiesGroup))
				node.QueueFree();

			// Remove all gold
			foreach (Node node in GetTree().GetNodesInGroup(GameConstants.GoldGroup))
				node.QueueFree();

			// Remove all arrows
			foreach (Node node in GetTree().GetNodesInGroup(GameConstants.ArrowsGroup))
				node.QueueFree();

			// Remove all dynamite
			foreach (Node node in GetTree().GetNodesInGroup(GameConstants.DynamiteGroup))
				node.QueueFree();

			GD.Print("Cleaned up all entities for restart");
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

		// Day/Night cycle signals
		[Signal]
		public delegate void DayStartedEventHandler(int dayNumber);

		[Signal]
		public delegate void ShelterWarningEventHandler();

		[Signal]
		public delegate void NightStartedEventHandler();

		[Signal]
		public delegate void DayCompletedEventHandler(int dayNumber);

		[Signal]
		public delegate void PlayerEnteredShelterEventHandler();

		[Signal]
		public delegate void NightDamageTickEventHandler();
	}
}
