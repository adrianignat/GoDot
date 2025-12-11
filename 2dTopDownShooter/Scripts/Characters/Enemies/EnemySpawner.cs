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
		if (Game.Instance.CurrentDay >= 3 && GD.Randf() < TntGoblinSpawnChance)
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
		float viewLeft = center.X - halfView.X;
		float viewRight = center.X + halfView.X;
		float viewTop = center.Y - halfView.Y;
		float viewBottom = center.Y + halfView.Y;

		// Map bounds (playable area)
		float mapLeft = camera.LimitLeft;
		float mapRight = camera.LimitRight;
		float mapTop = camera.LimitTop;
		float mapBottom = camera.LimitBottom;

		// Spawn margin outside camera view
		float margin = GameConstants.SpawnMargin;

		// Collect available spawn edges (parts of map outside camera view)
		var edges = new List<(float x1, float y1, float x2, float y2)>();

		// Left edge: map left to camera left (if there's space)
		if (viewLeft - margin > mapLeft)
			edges.Add((mapLeft, Mathf.Max(mapTop, viewTop), viewLeft - margin, Mathf.Min(mapBottom, viewBottom)));

		// Right edge: camera right to map right (if there's space)
		if (viewRight + margin < mapRight)
			edges.Add((viewRight + margin, Mathf.Max(mapTop, viewTop), mapRight, Mathf.Min(mapBottom, viewBottom)));

		// Top edge: map top to camera top (if there's space)
		if (viewTop - margin > mapTop)
			edges.Add((Mathf.Max(mapLeft, viewLeft), mapTop, Mathf.Min(mapRight, viewRight), viewTop - margin));

		// Bottom edge: camera bottom to map bottom (if there's space)
		if (viewBottom + margin < mapBottom)
			edges.Add((Mathf.Max(mapLeft, viewLeft), viewBottom + margin, Mathf.Min(mapRight, viewRight), mapBottom));

		// If no edges available (player view covers entire map), spawn at map center
		if (edges.Count == 0)
			return new Vector2((mapLeft + mapRight) / 2, (mapTop + mapBottom) / 2);

		// Pick random edge weighted by area
		var areas = new List<float>();
		float totalArea = 0;
		foreach (var (x1, y1, x2, y2) in edges)
		{
			float area = (x2 - x1) * (y2 - y1);
			areas.Add(area);
			totalArea += area;
		}

		float pick = (float)GD.RandRange(0, totalArea);
		float cumulative = 0;
		int edgeIndex = 0;
		for (int i = 0; i < areas.Count; i++)
		{
			cumulative += areas[i];
			if (pick <= cumulative)
			{
				edgeIndex = i;
				break;
			}
		}

		// Random point within selected edge rect
		var (ex1, ey1, ex2, ey2) = edges[edgeIndex];
		return new Vector2(
			(float)GD.RandRange(ex1, ex2),
			(float)GD.RandRange(ey1, ey2)
		);
	}
}
