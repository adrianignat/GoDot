using dTopDownShooter.Scripts.UI;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace dTopDownShooter.Scripts.Events
{
	public partial class GatherLumberEvent : GameEvent
	{
		private const int TargetLumberCount = 5;
		private const float MinDistance = 650f;
		private const float PickupRadius = 75f;
		private const float DropoffRadius = 110f;

		public override string EventId => "gather_lumber";
		public override string DisplayName => "Gather Lumber";

		private readonly List<LumberBundle> _bundles = new();
		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
		private Vector2 _dropoffPosition;
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
			var dropoff = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, MinDistance);
			if (!dropoff.HasValue)
			{
				FailEvent();
				return;
			}

			_dropoffPosition = dropoff.Value;
			SpawnBundles(playerPos);
			SetObjective($"Gather {TargetLumberCount} bundles of lumber");
			UpdateMarker();
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

			if (_collectedCount >= TargetLumberCount && player.GlobalPosition.DistanceTo(_dropoffPosition) <= DropoffRadius)
				CompleteEvent();
		}

		private void SpawnBundles(Vector2 playerPos)
		{
			var markers = _mapGenerator.GetBuildingSpawnMarkers()
				.Where(pos => pos.DistanceTo(playerPos) >= MinDistance * 0.5f)
				.Take(TargetLumberCount)
				.ToList();

			if (markers.Count < TargetLumberCount)
				markers = _mapGenerator.GetBuildingSpawnMarkers().Take(TargetLumberCount).ToList();

			for (int i = 0; i < markers.Count; i++)
			{
				var bundle = new LumberBundle();
				bundle.GlobalPosition = markers[i] + new Vector2(20 * i, -15 * i);
				bundle.Collected += OnBundleCollected;
				_mapGenerator.AddChild(bundle);
				_bundles.Add(bundle);
			}
		}

		private void OnBundleCollected(LumberBundle bundle)
		{
			_collectedCount++;
			if (_collectedCount < TargetLumberCount)
			{
				SetObjective($"Gather {TargetLumberCount - _collectedCount} more bundles of lumber");
			}
			else
			{
				SetObjective("Take the lumber to the drop-off point");
			}
			UpdateMarker();
		}

		private void UpdateMarker()
		{
			if (_eventMarker == null)
				return;

			if (_collectedCount >= TargetLumberCount)
			{
				_eventMarker.Configure("Drop-off", Colors.SaddleBrown);
				_eventMarker.SetTarget(_dropoffPosition);
				return;
			}

			var nextBundle = _bundles.FirstOrDefault(b => b != null && GodotObject.IsInstanceValid(b) && !b.IsCollected);
			_eventMarker.Configure("Lumber", Colors.SaddleBrown);
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
