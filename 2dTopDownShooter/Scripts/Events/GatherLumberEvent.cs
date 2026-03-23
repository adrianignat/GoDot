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
		private const float MinDistance = 650f;
		private const float PickupRadius = 75f;

		public override string EventId => "gather_lumber";
		public override string DisplayName => "Gather Lumber";

		private readonly List<LumberBundle> _bundles = new();
		private readonly Queue<Vector2> _remainingSpawnPositions = new();
		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
		private int _collectedCount;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "LumberMarker";
			Game.Instance.AddChild(_eventMarker);

			if (_mapGenerator == null)
			{
				FailEvent();
				return;
			}

			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			PrepareSpawnQueue(playerPos);
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

		private void PrepareSpawnQueue(Vector2 playerPos)
		{
			var positions = _mapGenerator.GetBuildingSpawnMarkers()
				.Where(pos => pos.DistanceTo(playerPos) >= MinDistance * 0.45f)
				.Take(TargetLumberCount)
				.ToList();

			if (positions.Count < TargetLumberCount)
			{
				positions = _mapGenerator.GetBuildingSpawnMarkers().Take(TargetLumberCount).ToList();
			}

			for (int i = 0; i < positions.Count; i++)
				_remainingSpawnPositions.Enqueue(positions[i] + new Vector2((i % 2) * 28, (i % 3) * -18));
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

			var nextBundle = _bundles.FirstOrDefault(b => b != null && GodotObject.IsInstanceValid(b) && !b.IsCollected);
			if (nextBundle != null)
				_eventMarker.SetTarget(nextBundle);
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
