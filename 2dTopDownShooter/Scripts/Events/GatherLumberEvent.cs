using dTopDownShooter.Scripts.UI;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace dTopDownShooter.Scripts.Events
{
	public partial class GatherLumberEvent : GameEvent
	{
		private const int TargetLumberCount = 8;
		private const int SimultaneousSpawnCount = 5;
		private const float PickupRadius = 75f;
		private const float SpawnClearance = 78f;
		private const float StructureAvoidanceRadius = 140f;
		private const int MaxSpawnAttemptsPerBundle = 40;

		public override string EventId => "gather_lumber";
		public override string DisplayName => "Gather Lumber";
		public override ushort CompletionRewardGold => 12;

		private readonly List<LumberBundle> _bundles = new();
		private readonly Queue<Vector2> _remainingSpawnPositions = new();
		private readonly List<Vector2> _reservedPositions = new();
		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;
		private int _collectedCount;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "LumberMarker";
			_eventMarker.SetNavigationVisible(false);
			Game.Instance.AddChild(_eventMarker);

			if (_mapGenerator == null)
			{
				FailEvent();
				return;
			}

			PrepareSpawnQueue();
			SpawnAvailableLumber();
			UpdateProgressUI();
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			var player = Game.Instance.Player;
			if (player == null)
				return;

			foreach (var bundle in _bundles.Where(b => b != null && GodotObject.IsInstanceValid(b)).ToList())
			{
				if (!bundle.IsCollected && bundle.GlobalPosition.DistanceTo(player.GlobalPosition) <= PickupRadius)
					bundle.Collect();
			}
		}

		private void PrepareSpawnQueue()
		{
			_reservedPositions.Clear();

			for (int i = 0; i < TargetLumberCount; i++)
			{
				var spawnPosition = FindValidRandomSpawnPosition();
				if (!spawnPosition.HasValue)
					break;

				_reservedPositions.Add(spawnPosition.Value);
				_remainingSpawnPositions.Enqueue(spawnPosition.Value);
			}
		}

		private Vector2? FindValidRandomSpawnPosition()
		{
			Rect2 playableArea = _mapGenerator.GetPlayableAreaBounds().Grow(-SpawnClearance);
			var housePositions = _mapGenerator.GetHousePositions();
			var buildingMarkers = _mapGenerator.GetBuildingSpawnMarkers();

			for (int attempt = 0; attempt < MaxSpawnAttemptsPerBundle; attempt++)
			{
				Vector2 candidate = new Vector2(
					(float)GD.RandRange(playableArea.Position.X, playableArea.End.X),
					(float)GD.RandRange(playableArea.Position.Y, playableArea.End.Y));

				if (Game.Instance.IsInNoSpawnZone(candidate))
					continue;

				if (housePositions.Any(house => house.DistanceTo(candidate) < StructureAvoidanceRadius))
					continue;

				if (buildingMarkers.Any(marker => marker.DistanceTo(candidate) < StructureAvoidanceRadius))
					continue;

				if (_reservedPositions.Any(existing => existing.DistanceTo(candidate) < StructureAvoidanceRadius))
					continue;

				if (HasMapCollisionNearby(candidate, SpawnClearance))
					continue;

				return candidate;
			}

			return null;
		}

		private bool HasMapCollisionNearby(Vector2 position, float radius)
		{
			var spaceState = Game.Instance.GetWorld2D().DirectSpaceState;
			var shape = new CircleShape2D { Radius = radius };
			var query = new PhysicsShapeQueryParameters2D
			{
				Shape = shape,
				Transform = new Transform2D(0f, position),
				CollisionMask = GameConstants.MapCollisionLayer,
				CollideWithBodies = true,
				CollideWithAreas = false
			};

			var results = spaceState.IntersectShape(query, 1);
			return results.Count > 0;
		}

		private void SpawnAvailableLumber()
		{
			int activeCount = _bundles.Count(b => b != null && GodotObject.IsInstanceValid(b) && !b.IsCollected);
			while (activeCount < SimultaneousSpawnCount && _remainingSpawnPositions.Count > 0)
			{
				var bundle = new LumberBundle();
				bundle.GlobalPosition = _remainingSpawnPositions.Dequeue();
				bundle.Collected += OnBundleCollected;
				_mapGenerator.AddChild(bundle);
				_bundles.Add(bundle);
				activeCount++;
			}
		}

		private void OnBundleCollected(LumberBundle bundle)
		{
			_collectedCount++;
			if (_collectedCount >= TargetLumberCount)
			{
				UpdateProgressUI();
				CompleteEvent();
				return;
			}

			SpawnAvailableLumber();
			UpdateProgressUI();
		}

		private void UpdateProgressUI()
		{
			SetObjective($"Collect lumber {_collectedCount}/{TargetLumberCount}");
			if (_eventMarker == null)
				return;

			_eventMarker.Configure("Lumber", Colors.SaddleBrown);
			_eventMarker.SetStatus($"Collected {_collectedCount}/{TargetLumberCount}");
			_eventMarker.SetProgress(_collectedCount, TargetLumberCount);
		}

		protected override void OnEventComplete()
		{
			_eventMarker?.Hide();
		}

		protected override void OnEventFail()
		{
			_eventMarker?.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();

			foreach (var bundle in _bundles)
			{
				if (bundle != null && GodotObject.IsInstanceValid(bundle))
					bundle.QueueFree();
			}
		}
	}
}
