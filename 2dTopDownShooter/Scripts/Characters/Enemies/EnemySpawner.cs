using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Spawners;
using Godot;
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

	/// <summary>
	/// Chance (0.0 to 1.0) that a spawned enemy will be a TNT goblin instead of a regular goblin.
	/// </summary>
	[Export]
	public float TntGoblinSpawnChance = 0.15f;

	private List<EnemyTier> _availableTiers = new() { EnemyTier.Blue };
	private int _nextTierIndex = 1; // Start at Red (index 1 in enum)
	private float _initialSpawnRate;
	private float _tierTimer;
	private float _spawnRateIncreaseTimer;
	private const float SpawnRateIncreaseInterval = 30f;
	private PackedScene _tntGoblinScene;

	public override void _Ready()
	{
		base._Ready();
		_initialSpawnRate = ObjectsPerSecond;
		_tntGoblinScene = GD.Load<PackedScene>("res://Entities/Characters/Enemies/tnt_goblin.tscn");

		// Initialize manual timers (pause-aware)
		_spawnRateIncreaseTimer = SpawnRateIncreaseInterval;
		_tierTimer = TierIntroductionInterval;

		// Subscribe to day transition signals
		Game.Instance.DayStarted += OnDayStarted;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		// Don't update timers when paused
		if (Game.Instance.IsPaused)
			return;

		// Spawn rate increase timer
		_spawnRateIncreaseTimer -= (float)delta;
		if (_spawnRateIncreaseTimer <= 0)
		{
			ObjectsPerSecond *= SpawnIncreaseRate;
			_spawnRateIncreaseTimer = SpawnRateIncreaseInterval;
		}

		// Tier introduction timer
		_tierTimer -= (float)delta;
		if (_tierTimer <= 0)
		{
			OnTierTimerTimeout();
			_tierTimer = TierIntroductionInterval;
		}
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
		_tierTimer = TierIntroductionInterval;

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

	protected override Node GetSpawnParent()
	{
		// Spawn enemies to EntityLayer for Y-sorting
		return Game.Instance.EntityLayer;
	}

	protected override void Spawn()
	{
		// TNT goblins only appear starting day 3
		if (GD.Randf() < TntGoblinSpawnChance)
		{
			SpawnTntGoblin();
		}
		else
		{
			// Call base implementation for regular enemy
			base.Spawn();
		}
	}

	private void SpawnTntGoblin()
	{
		var tntGoblin = _tntGoblinScene.Instantiate<TntGoblin>();
		tntGoblin.GlobalPosition = GetLocation();
		tntGoblin.Tier = GetWeightedRandomTier();
		GetSpawnParent().AddChild(tntGoblin);
	}

	protected override void InitializeSpawnedObject(Enemy enemy)
	{
		enemy.Tier = GetWeightedRandomTier();
	}

	private EnemyTier GetWeightedRandomTier()
	{
		if (_availableTiers.Count == 1)
			return _availableTiers[0];

		int currentDay = Game.Instance.CurrentDay;

		// Build weighted list: higher tiers get more weight on later days
		// Base weight = 10, bonus per tier per day = (tierIndex * dayBonus)
		var weights = new List<int>();
		int totalWeight = 0;

		for (int i = 0; i < _availableTiers.Count; i++)
		{
			// Higher index = higher tier
			// Day 1: all equal (base 10)
			// Day 2+: higher tiers get bonus weight
			int tierBonus = i * (currentDay - 1) * 2; // 2 extra weight per tier level per day
			int weight = 10 + tierBonus;
			weights.Add(weight);
			totalWeight += weight;
		}

		// Weighted random selection
		int roll = GD.RandRange(0, totalWeight - 1);
		int cumulative = 0;

		for (int i = 0; i < _availableTiers.Count; i++)
		{
			cumulative += weights[i];
			if (roll < cumulative)
				return _availableTiers[i];
		}

		// Fallback
		return _availableTiers[_availableTiers.Count - 1];
	}

	public override Vector2 GetLocation()
	{
		var camera = Game.Instance.Player.GetNode<Camera2D>("Camera2D");

		// Visible area
		Vector2 center = camera.GetScreenCenterPosition();
		Vector2 halfView = GetViewportRect().Size / camera.Zoom / 2;

		// Spawn margin outside camera view
		float margin = GameConstants.SpawnMargin;

		// Spawn distance range (from just outside camera to further out)
		float minSpawnDistance = margin;
		float maxSpawnDistance = margin + 200f; // Can spawn up to 200px beyond the camera edge

		// Try to find a valid spawn location
		for (int attempt = 0; attempt < GameConstants.MaxSpawnAttempts; attempt++)
		{
			Vector2 spawnPos = GetRandomSpawnPosition(center, halfView, minSpawnDistance, maxSpawnDistance);

			if (IsValidSpawnLocation(spawnPos))
				return spawnPos;
		}

		// Fallback: return a position outside camera regardless of collision
		return GetRandomSpawnPosition(center, halfView, minSpawnDistance, maxSpawnDistance);
	}

	private Vector2 GetRandomSpawnPosition(Vector2 center, Vector2 halfView, float minDistance, float maxDistance)
	{
		// Pick a random direction (angle in radians)
		float angle = (float)GD.RandRange(0, Mathf.Tau);

		// Pick which edge to spawn from based on angle
		// This ensures enemies spawn outside the camera view in all directions
		float cosAngle = Mathf.Cos(angle);
		float sinAngle = Mathf.Sin(angle);

		// Calculate intersection with camera edge rectangle
		// We need to find where a ray from center at 'angle' intersects the camera boundary
		float tX = cosAngle != 0 ? (halfView.X + minDistance) / Mathf.Abs(cosAngle) : float.MaxValue;
		float tY = sinAngle != 0 ? (halfView.Y + minDistance) / Mathf.Abs(sinAngle) : float.MaxValue;

		// Use the smaller t to get the edge intersection
		float t = Mathf.Min(tX, tY);

		// Add random distance beyond the edge
		float extraDistance = (float)GD.RandRange(0, maxDistance - minDistance);
		t += extraDistance;

		// Calculate spawn position
		return new Vector2(
			center.X + cosAngle * t,
			center.Y + sinAngle * t
		);
	}

	private bool IsValidSpawnLocation(Vector2 position)
	{
		// Use physics query to check for collisions at spawn point
		var spaceState = GetWorld2D().DirectSpaceState;

		// Create a small circle shape for the query
		var shape = new CircleShape2D();
		shape.Radius = 16f; // Small radius to check for obstacles

		var query = new PhysicsShapeQueryParameters2D();
		query.Shape = shape;
		query.Transform = new Transform2D(0, position);
		query.CollisionMask = 1; // Layer 1 = Map (terrain, buildings, trees)

		var results = spaceState.IntersectShape(query, 1);

		// Valid if no collisions found
		return results.Count == 0;
	}
}
