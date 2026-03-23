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

		public override string EventId => "gather_pink_sheep";
		public override string DisplayName => "Gather Pink Sheep";

		private readonly List<EventSheep> _sheep = new();
		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
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
			var dropoffMarker = _mapGenerator.GetSpawnMarkerAwayFrom(flockMarker ?? playerPos, MinDistance * 0.6f);
			if (!flockMarker.HasValue || !dropoffMarker.HasValue)
			{
				FailEvent();
				return;
			}

			_dropoffPosition = dropoffMarker.Value;
			SpawnSheep(flockMarker.Value);
			SetObjective($"Gather {TargetSheepCount} pink sheep");
			UpdateMarker();
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

			foreach (var sheep in _sheep.Where(s => s != null && GodotObject.IsInstanceValid(s) && s.IsCollected).ToList())
			{
				if (sheep.GlobalPosition.DistanceTo(_dropoffPosition) <= DropoffRadius)
				{
					_deliveredCount++;
					sheep.MarkDelivered();
				}
			}

			if (_deliveredCount >= TargetSheepCount)
				CompleteEvent();
		}

		private void SpawnSheep(Vector2 flockPosition)
		{
			for (int i = 0; i < TargetSheepCount; i++)
			{
				var sheepBody = _mapGenerator.SpawnSheepAt(flockPosition + new Vector2(70 * i, 30 * (i % 2)));
				var sheep = sheepBody as EventSheep;
				if (sheep == null)
					continue;

				sheep.Collected += OnSheepCollected;
				_sheep.Add(sheep);
			}
		}

		private void OnSheepCollected(EventSheep sheep)
		{
			_collectedCount++;
			if (_collectedCount < TargetSheepCount)
				SetObjective($"Gather {TargetSheepCount - _collectedCount} more pink sheep");
			else
				SetObjective("Take the sheep to the drop-off point");
			UpdateMarker();
		}

		private void UpdateMarker()
		{
			if (_eventMarker == null)
				return;

			if (_collectedCount >= TargetSheepCount)
			{
				_eventMarker.Configure("Pen", Colors.HotPink);
				_eventMarker.SetTarget(_dropoffPosition);
				return;
			}

			var nextSheep = _sheep.FirstOrDefault(s => s != null && GodotObject.IsInstanceValid(s) && !s.IsCollected);
			_eventMarker.Configure("Sheep", Colors.HotPink);
			if (nextSheep != null)
				_eventMarker.SetTarget(nextSheep);
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

			foreach (var sheep in _sheep)
			{
				if (sheep != null && GodotObject.IsInstanceValid(sheep))
					sheep.QueueFree();
			}
		}
	}
}
