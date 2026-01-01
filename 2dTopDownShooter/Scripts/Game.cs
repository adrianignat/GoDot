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

		internal bool IsPaused
		{
			get => GetTree()?.Paused ?? false;
			set
			{
				if (GetTree() != null)
					GetTree().Paused = value;
			}
		}

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

		public Node2D EntityLayer { get; set; }

		public Player Player { get; set; }

		public override void _EnterTree()
		{
			// Set singleton BEFORE any child _Ready() methods run
			// This ensures Game.Instance returns this node when children connect to signals
			_gameInstance = this;

			// Initialize core references early so other scripts can use them
			// _EnterTree runs before children's _Ready methods
			MainWindow = this;
			EntityLayer = GetNode<Node2D>("MapGenerator");
			Player = EntityLayer.GetNode<Player>("Player");
		}

		public override void _Ready()
		{
			// Position player at map center if MapGenerator exists
			var mapGenerator = MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			if (mapGenerator != null)
			{
				Player.Position = mapGenerator.GetPlayerSpawnPosition();

				// Update camera limits to the playable area (not full map)
				// This keeps the camera within the player's playable zone
				var camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
				if (camera != null)
				{
					var playableArea = mapGenerator.GetPlayableAreaBounds();
					camera.LimitLeft = (int)playableArea.Position.X;
					camera.LimitTop = (int)playableArea.Position.Y;
					camera.LimitRight = (int)(playableArea.Position.X + playableArea.Size.X);
					camera.LimitBottom = (int)(playableArea.Position.Y + playableArea.Size.Y);
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

		/// <summary>
		/// Check if a world position is in a no-spawn zone (water, etc.) by querying the NoSpawn TileMapLayer.
		/// </summary>
		public bool IsInNoSpawnZone(Vector2 worldPosition)
		{
			var mapGenerator = MainWindow.GetNodeOrNull<Node2D>("MapGenerator");
			if (mapGenerator == null)
				return false;

			// Check all map pieces for NoSpawn layers
			foreach (Node child in mapGenerator.GetChildren())
			{
				if (child is not Node2D mapPiece)
					continue;

				var noSpawnLayer = mapPiece.GetNodeOrNull<TileMapLayer>("NoSpawn");
				if (noSpawnLayer == null)
					continue;

				// Convert world position to tile coordinates
				Vector2I cell = noSpawnLayer.LocalToMap(noSpawnLayer.ToLocal(worldPosition));

				// If there's a tile at this position, it's a no-spawn zone
				if (noSpawnLayer.GetCellSourceId(cell) != -1)
					return true;
			}

			return false;
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
