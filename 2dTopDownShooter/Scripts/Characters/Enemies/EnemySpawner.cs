using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Spawners;
using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class EnemySpawner : Spawner<Enemy>
{
	[Export]
	public float SpawnIncreaseRate = 1.1f;

	/// <summary>
	/// Time in seconds before each new enemy tier is introduced.
	/// Index 0 = Red (2 shots), Index 1 = Purple (3 shots), Index 2 = Yellow (4 shots)
	/// Blue enemies spawn from the start.
	/// </summary>
	[Export]
	public float TierIntroductionInterval = 60f;

	/// <summary>
	/// How much to reduce spawn rate on day transition (percentage kept, e.g., 0.7 = 70%)
	/// </summary>
	[Export]
	public float DayTransitionSpawnRetention = 0.7f;

	private Camera2D _camera;
	private List<EnemyTier> _availableTiers = new() { EnemyTier.Blue };
	private int _nextTierIndex = 1; // Start at Red (index 1 in enum)
	private float _initialSpawnRate;
	private Timer _tierTimer;

	Array<Node> _validSpawnTiles;

	public override void _Ready()
	{
		base._Ready();
		_initialSpawnRate = ObjectsPerSecond;

		var timer = GetNode<Timer>("IncreaseSpawnRate");
		timer.Timeout += () => ObjectsPerSecond *= SpawnIncreaseRate;

		_camera = GetTree().Root.GetNode("main").GetNode("Player").GetNode<Camera2D>("Camera2D");
		_validSpawnTiles = GetTree().GetNodesInGroup("validSpawnLocation");

		// Setup timer for introducing new enemy tiers
		_tierTimer = new Timer();
		_tierTimer.WaitTime = TierIntroductionInterval;
		_tierTimer.OneShot = false;
		_tierTimer.Timeout += OnTierTimerTimeout;
		AddChild(_tierTimer);
		_tierTimer.Start();

		// Subscribe to day transition signals
		Game.Instance.DayStarted += OnDayStarted;
	}

	private void OnDayStarted(int dayNumber)
	{
		if (dayNumber == 1)
			return; // First day, no modifications needed

		// Reduce spawn rate but don't reset to initial
		ObjectsPerSecond *= DayTransitionSpawnRetention;

		// Ensure spawn rate doesn't go below initial
		if (ObjectsPerSecond < _initialSpawnRate)
			ObjectsPerSecond = _initialSpawnRate;

		// Reset enemy tier by one level
		ResetTierByOne();

		// Restart tier timer for new day
		_tierTimer.Stop();
		_tierTimer.Start();

		// Refresh valid spawn tiles (map regenerated)
		_validSpawnTiles = GetTree().GetNodesInGroup("validSpawnLocation");

		GD.Print($"Day {dayNumber}: Spawn rate = {ObjectsPerSecond}, Available tiers = {string.Join(", ", _availableTiers)}");
	}

	private void ResetTierByOne()
	{
		if (_availableTiers.Count > 1)
		{
			// Remove highest tier
			_availableTiers.RemoveAt(_availableTiers.Count - 1);
		}

		// Set _nextTierIndex to the highest current tier
		// This way the timer will add the next tier correctly
		if (_availableTiers.Count > 0)
		{
			_nextTierIndex = (int)_availableTiers[_availableTiers.Count - 1];
		}
		else
		{
			_nextTierIndex = 0; // Will become 1 (Blue) on first timeout
		}
	}

	private void OnTierTimerTimeout()
	{
		// Add next tier if available (Red=2, Purple=3, Yellow=4)
		_nextTierIndex++;
		if (_nextTierIndex <= 4)
		{
			var newTier = (EnemyTier)_nextTierIndex;
			_availableTiers.Add(newTier);
			GD.Print($"New enemy tier introduced: {newTier}");
		}
	}

	protected override void InitializeSpawnedObject(Enemy enemy)
	{
		var randomTier = _availableTiers[GD.RandRange(0, _availableTiers.Count - 1)];
		enemy.Tier = randomTier;
	}

	public override Vector2 GetLocation()
	{
		// Refresh spawn tiles if needed (after map regeneration)
		RefreshValidSpawnTilesIfNeeded();

		bool isValidSpawn = false;
		int attempts = 0;
		const int maxAttempts = 100;

		Vector2 spawnPosition = new();
		while (!isValidSpawn && attempts < maxAttempts)
		{
			attempts++;
			// Get a random position outside the camera view
			spawnPosition = GetRandomSpawnPositionOutsideCamera(_camera);

			foreach (TileMapLayer tilemap in _validSpawnTiles)
			{
				if (IsWithinTileMapLayer(tilemap, spawnPosition))
				{
					isValidSpawn = true;
					break;
				}
			}
		}

		// Fallback if no valid position found
		if (!isValidSpawn)
		{
			spawnPosition = GetRandomSpawnPositionOutsideCamera(_camera);
		}

		return spawnPosition;
	}

	private void RefreshValidSpawnTilesIfNeeded()
	{
		// Check if tiles need refreshing (empty or contains freed nodes)
		bool needsRefresh = _validSpawnTiles == null || _validSpawnTiles.Count == 0;

		if (!needsRefresh)
		{
			// Check if the first tile is still valid (not freed)
			foreach (var node in _validSpawnTiles)
			{
				if (!GodotObject.IsInstanceValid(node))
				{
					needsRefresh = true;
					break;
				}
			}
		}

		if (needsRefresh)
		{
			_validSpawnTiles = GetTree().GetNodesInGroup("validSpawnLocation");
			GD.Print($"Refreshed valid spawn tiles: {_validSpawnTiles.Count} found");
		}
	}

	public Vector2 GetRandomSpawnPositionOutsideCamera(Camera2D camera)
	{
		// Use GetScreenCenterPosition() to get the actual view center (accounts for camera limits)
		Vector2 cameraPosition = camera.GetScreenCenterPosition();
		Vector2 screenSize = GetViewportRect().Size;
		Vector2 cameraZoom = camera.Zoom;
		Vector2 viewportSize = screenSize / cameraZoom;

		// Get the boundaries of the visible area
		float leftBound = cameraPosition.X - viewportSize.X / 2;
		float rightBound = cameraPosition.X + viewportSize.X / 2;
		float topBound = cameraPosition.Y - viewportSize.Y / 2;
		float bottomBound = cameraPosition.Y + viewportSize.Y / 2;

		// Spawn margin - distance outside the visible area to spawn enemies
		float spawnMargin = 50f;

		// Randomly choose which side to spawn the enemy on (left, right, top, or bottom)
		uint side = GD.Randi() % 4;  // Random number between 0 and 3

		Vector2 spawnPosition = new();

		switch (side)
		{
			case 0: // Spawn on the left
				spawnPosition.X = leftBound - spawnMargin;
				spawnPosition.Y = (float)GD.RandRange(topBound - spawnMargin, bottomBound + spawnMargin);
				break;
			case 1: // Spawn on the right
				spawnPosition.X = rightBound + spawnMargin;
				spawnPosition.Y = (float)GD.RandRange(topBound - spawnMargin, bottomBound + spawnMargin);
				break;
			case 2: // Spawn on the top
				spawnPosition.X = (float)GD.RandRange(leftBound - spawnMargin, rightBound + spawnMargin);
				spawnPosition.Y = topBound - spawnMargin;
				break;
			case 3: // Spawn on the bottom
				spawnPosition.X = (float)GD.RandRange(leftBound - spawnMargin, rightBound + spawnMargin);
				spawnPosition.Y = bottomBound + spawnMargin;
				break;
		}

		return spawnPosition;
	}

	private static bool IsWithinTileMapLayer(TileMapLayer tilemapLayer, Vector2 position)
	{
		// Convert world position to map coordinates (tile position)
		Vector2I mapCoords = tilemapLayer.LocalToMap(position);

		// Check if the map coordinates are within the tilemap bounds
		// You can adjust the bounds check based on your game requirements
		var tilemapBounds = tilemapLayer.GetCellTileData(mapCoords);

		return tilemapBounds != null;
	}
}
