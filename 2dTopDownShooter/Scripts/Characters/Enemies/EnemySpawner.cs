using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class EnemySpawner : Spawner<Enemy>
{
	[Export]
	Node2D[] spawnPoints;

	public override void _Ready()
	{
		base._Ready();
		var timer = GetNode<Timer>("IncreaseSpawnRate");
		timer.Timeout += () => ObjectsPerSecond *= 1.2f;
	}

	public override Vector2 GetLocation()
	{
		RandomNumberGenerator rng = new();
		Vector2 location = spawnPoints[rng.Randi() % spawnPoints.Length].GlobalPosition;
		return location;
	}
}
