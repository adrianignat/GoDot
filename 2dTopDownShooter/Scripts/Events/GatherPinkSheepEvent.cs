using dTopDownShooter.Scripts.UI;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace dTopDownShooter.Scripts.Events
{
	public partial class GatherPinkSheepEvent : GameEvent
	{
		private const int TargetSheepCount = 3;
		private const float MinDistance = 700f;
		private const float CollectionRadius = 80f;
		private const float DropoffRadius = 130f;
		private const float PenSeparationDistance = 650f;
		private const float SheepSpawnMaxRadius = 420f;
		private const float SheepSpawnSeparation = 180f;
		private const float SheepMaxDistanceBetweenSheep = 420f;
		private const float SheepSpawnClearance = 72f;
		private const int MaxSheepSpawnAttempts = 200;

		public override string EventId => "gather_pink_sheep";
		public override string DisplayName => "Gather Pink Sheep";
		public override ushort CompletionRewardGold => 12;

		private readonly List<EventSheep> _sheep = new();
		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;
		private EventPen _pen;
		private Vector2 _dropoffPosition;
		private int _collectedCount;
		private int _deliveredCount;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "PinkSheepMarker";
			Game.Instance.AddChild(_eventMarker);

			if (_mapGenerator == null)
			{
				FailEvent();
				return;
			}

			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			var flockMarker = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, MinDistance);
			var dropoffMarker = flockMarker.HasValue
				? _mapGenerator.GetSpawnMarkerAwayFrom(flockMarker.Value, PenSeparationDistance)
				: null;
			if (!flockMarker.HasValue || !dropoffMarker.HasValue)
			{
				FailEvent();
				return;
			}

			_dropoffPosition = dropoffMarker.Value;
			SpawnPen();
			if (!SpawnSheep(flockMarker.Value))
			{
				FailEvent();
				return;
			}
			UpdateUI();
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			var player = Game.Instance.Player;
			if (player == null)
				return;

			foreach (var sheep in _sheep.Where(s => s != null && GodotObject.IsInstanceValid(s)).ToList())
			{
				if (!sheep.IsCollected && sheep.GlobalPosition.DistanceTo(player.GlobalPosition) <= CollectionRadius)
					sheep.Collect();
			}

			if (_collectedCount < TargetSheepCount)
				return;

			foreach (var sheep in _sheep.Where(s => s != null && GodotObject.IsInstanceValid(s) && s.IsCollected && !s.IsDelivered).ToList())
			{
				if (sheep.GlobalPosition.DistanceTo(_dropoffPosition) <= DropoffRadius)
				{
					_deliveredCount++;
					sheep.MarkDelivered();
					UpdateUI();
				}
			}

			if (_deliveredCount >= TargetSheepCount)
				CompleteEvent();
		}

		private void SpawnPen()
		{
			_pen = new EventPen();
			_pen.GlobalPosition = _dropoffPosition;
			_mapGenerator.AddChild(_pen);
		}

		private bool SpawnSheep(Vector2 flockPosition)
		{
			var reservedPositions = new List<Vector2>();

			for (int i = 0; i < TargetSheepCount; i++)
			{
				var spawnPosition = FindValidSheepSpawnPosition(flockPosition, reservedPositions);
				if (!spawnPosition.HasValue)
					return false;

				reservedPositions.Add(spawnPosition.Value);
				var sheepBody = _mapGenerator.SpawnSheepAt(spawnPosition.Value);
				var sheep = sheepBody as EventSheep;
				if (sheep == null)
					return false;

				sheep.Collected += OnSheepCollected;
				_sheep.Add(sheep);
			}

			return true;
		}

		private Vector2? FindValidSheepSpawnPosition(Vector2 flockPosition, IEnumerable<Vector2> reservedPositions)
		{
			Rect2 playableArea = _mapGenerator.GetPlayableAreaBounds().Grow(-SheepSpawnClearance);

			for (int attempt = 0; attempt < MaxSheepSpawnAttempts; attempt++)
			{
				float angle = (float)GD.RandRange(0, Mathf.Tau);
				float distance = (float)GD.RandRange(0f, SheepSpawnMaxRadius);
				Vector2 candidate = flockPosition + Vector2.Right.Rotated(angle) * distance;

				if (!playableArea.HasPoint(candidate))
					continue;

				if (Game.Instance.IsInNoSpawnZone(candidate))
					continue;

				if (reservedPositions.Any(existing => existing.DistanceTo(candidate) < SheepSpawnSeparation))
					continue;

				if (reservedPositions.Any(existing => existing.DistanceTo(candidate) > SheepMaxDistanceBetweenSheep))
					continue;

				if (HasMapCollisionNearby(candidate, SheepSpawnClearance))
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

		private void OnSheepCollected(EventSheep sheep)
		{
			_collectedCount++;
			UpdateUI();
		}

		private void UpdateUI()
		{
			if (_collectedCount < TargetSheepCount)
			{
				SetObjective($"Collect pink sheep {_collectedCount}/{TargetSheepCount}");
				_eventMarker?.Configure("Pink Sheep", Colors.HotPink);
				_eventMarker?.SetStatus($"Collected {_collectedCount}/{TargetSheepCount}");
				_eventMarker?.SetProgress(_collectedCount, TargetSheepCount);
				var nextSheep = _sheep.FirstOrDefault(s => s != null && GodotObject.IsInstanceValid(s) && !s.IsCollected);
				if (nextSheep != null)
					_eventMarker?.SetTarget(nextSheep);
				return;
			}

			SetObjective($"Bring sheep to pen {_deliveredCount}/{TargetSheepCount}");
			_eventMarker?.Configure("Sheep Pen", Colors.HotPink);
			_eventMarker?.SetStatus($"Penned {_deliveredCount}/{TargetSheepCount}");
			_eventMarker?.SetProgress(_deliveredCount, TargetSheepCount);
			if (_pen != null && GodotObject.IsInstanceValid(_pen))
				_eventMarker?.SetTarget(_pen);
			else
				_eventMarker?.SetTarget(_dropoffPosition);
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
			if (_pen != null && GodotObject.IsInstanceValid(_pen))
				_pen.QueueFree();

			foreach (var sheep in _sheep)
			{
				if (sheep != null && GodotObject.IsInstanceValid(sheep))
					sheep.QueueFree();
			}
		}
	}
}
