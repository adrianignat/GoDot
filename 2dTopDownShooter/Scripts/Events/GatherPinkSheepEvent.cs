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
			var dropoffMarker = _mapGenerator.GetSpawnMarkerAwayFrom(flockMarker ?? playerPos, MinDistance * 0.6f);
			if (!flockMarker.HasValue || !dropoffMarker.HasValue)
			{
				FailEvent();
				return;
			}

			_dropoffPosition = dropoffMarker.Value;
			SpawnPen();
			SpawnSheep(flockMarker.Value);
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

			foreach (var sheep in _sheep.Where(s => s != null && GodotObject.IsInstanceValid(s) && s.IsCollected).ToList())
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

		private void SpawnSheep(Vector2 flockPosition)
		{
			Vector2[] flockOffsets =
			{
				new Vector2(-130, -70),
				new Vector2(120, -10),
				new Vector2(10, 125)
			};

			for (int i = 0; i < TargetSheepCount; i++)
			{
				Vector2 spawnOffset = i < flockOffsets.Length
					? flockOffsets[i]
					: new Vector2(GD.RandRange(-130, 130), GD.RandRange(-130, 130));

				var sheepBody = _mapGenerator.SpawnSheepAt(flockPosition + spawnOffset);
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
