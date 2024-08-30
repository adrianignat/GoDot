using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class EnemySpawner : Spawner<Enemy>
{
	[Export]
	Node2D[] spawnPoints;

	[Export]
	public float SpawnIncreaseRate = 1.1f;

	public override void _Ready()
	{
		base._Ready();
		var timer = GetNode<Timer>("IncreaseSpawnRate");
		timer.Timeout += () => ObjectsPerSecond *= SpawnIncreaseRate;
	}

	public override Vector2 GetLocation()
	{
		RandomNumberGenerator rng = new();
		Vector2 location = spawnPoints[rng.Randi() % spawnPoints.Length].GlobalPosition;
		return location;
	}
}
