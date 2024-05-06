using Godot;
using System;

public partial class EnemySpawner : Node2D
{
	[Export]
	public PackedScene EnemyScene;
	[Export]
	Node2D[] spawnPoints;

	[Export]
	public float EnemiesPerSecond = 1f;

	float spawnRate = 1f;
	float timeUntilNextSpawn = 0f;

	public override void _Ready()
	{
		spawnRate = 1/EnemiesPerSecond;
	}

	public override void _Process(double delta)
	{
		if (timeUntilNextSpawn > spawnRate)
		{
			timeUntilNextSpawn = 0f;
			SpawnEnemy();
		}
		else
		{
			timeUntilNextSpawn += (float)delta;
		}
	}

	private void SpawnEnemy()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		Vector2 location = spawnPoints[rng.Randi() % spawnPoints.Length].GlobalPosition;
		var enemy = EnemyScene.Instantiate() as Enemy;
		enemy.GlobalPosition = location;
		GetTree().Root.AddChild(enemy);
	}
}
