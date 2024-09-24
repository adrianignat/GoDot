using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class EnemySpawner : Spawner<Enemy>
{
	[Export]
	public float SpawnIncreaseRate = 1.1f;

	private Camera2D _camera;

	public override void _Ready()
	{
		base._Ready();
		var timer = GetNode<Timer>("IncreaseSpawnRate");
		timer.Timeout += () => ObjectsPerSecond *= SpawnIncreaseRate;

		_camera = GetTree().Root.GetNode("main").GetNode("Player").GetNode<Camera2D>("Camera2D");
	}

	public override Vector2 GetLocation()
	{
		return GetRandomSpawnPositionOutsideCamera(_camera);
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
}
