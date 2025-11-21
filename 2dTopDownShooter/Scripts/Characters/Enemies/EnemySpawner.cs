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
	public float TierIntroductionInterval = 10f;

	private Camera2D _camera;
	private List<EnemyTier> _availableTiers = new() { EnemyTier.Blue };
	private int _nextTierIndex = 1; // Start at Red (index 1 in enum)

	Array<Node> _validSpawnTiles;

	public override void _Ready()
	{
		base._Ready();
		var timer = GetNode<Timer>("IncreaseSpawnRate");
		timer.Timeout += () => ObjectsPerSecond *= SpawnIncreaseRate;

		_camera = GetTree().Root.GetNode("main").GetNode("Player").GetNode<Camera2D>("Camera2D");
		_validSpawnTiles = GetTree().GetNodesInGroup("validSpawnLocation");

		// Setup timer for introducing new enemy tiers
		var tierTimer = new Timer();
		tierTimer.WaitTime = TierIntroductionInterval;
		tierTimer.OneShot = false;
		tierTimer.Timeout += OnTierTimerTimeout;
		AddChild(tierTimer);
		tierTimer.Start();
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

	protected override void Spawn()
	{
		var enemy = Scene.Instantiate<Enemy>();
		enemy.GlobalPosition = GetLocation();
		GetTree().Root.AddChild(enemy);

		// Initialize with a random tier from available tiers
		var randomTier = _availableTiers[GD.RandRange(0, _availableTiers.Count - 1)];
		enemy.Initialize(randomTier);
	}

	public override Vector2 GetLocation()
	{
		bool isValidSpawn = false;

		Vector2 spawnPosition = new();
		while (!isValidSpawn)
		{
			// Get a random position outside the camera view
			spawnPosition = GetRandomSpawnPositionOutsideCamera(_camera);

			foreach (TileMapLayer tilemap in _validSpawnTiles)
			{
				if (IsWithinTileMapLayer(tilemap, spawnPosition))
				{
					isValidSpawn = true;
					break;
				}
				else
				{
					isValidSpawn = false;
				}
			}
		}
		return spawnPosition;
	}

	public Vector2 GetRandomSpawnPositionOutsideCamera(Camera2D camera)
	{
		// Get the camera position and viewport size
		Vector2 cameraPosition = camera.GlobalPosition;
		Vector2 screenSize = GetViewportRect().Size;
		Vector2 cameraZoom = camera.Zoom;
		Vector2 viewportSize = screenSize / cameraZoom;

		// Get the boundaries of the visible area
		float leftBound = cameraPosition.X - viewportSize.X / 2;
		float rightBound = cameraPosition.X + viewportSize.X / 2;
		float topBound = cameraPosition.Y - viewportSize.Y / 2;
		float bottomBound = cameraPosition.Y + viewportSize.Y / 2;

		float spawnMargin = 10f;

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
