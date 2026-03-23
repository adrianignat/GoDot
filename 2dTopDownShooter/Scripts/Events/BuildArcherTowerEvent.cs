using dTopDownShooter.Scripts.UI;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class BuildArcherTowerEvent : GameEvent
	{
		private const float MinDistance = 700f;

		public override string EventId => "build_archer_tower";
		public override string DisplayName => "Build the Archer Tower";

		private MapGenerator _mapGenerator;
		private ArcheryRuins _ruins;
		private EventMarker _eventMarker;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "ArcherTowerMarker";
			Game.Instance.AddChild(_eventMarker);

			if (_mapGenerator == null)
			{
				FailEvent();
				return;
			}

			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			var marker = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, MinDistance);
			if (!marker.HasValue)
			{
				FailEvent();
				return;
			}

			_ruins = _mapGenerator.SpawnArcheryRuinsAt(marker.Value);
			if (_ruins == null)
			{
				FailEvent();
				return;
			}

			_ruins.Built += OnRuinsBuilt;
			SetObjective("Travel to the ruins and build the archer tower");
			_eventMarker.Configure("Archer Tower", Colors.Gold);
			_eventMarker.SetTarget(_ruins);
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			if (_ruins == null || !GodotObject.IsInstanceValid(_ruins))
				FailEvent();
		}

		private void OnRuinsBuilt(ArcheryRuins ruins)
		{
			CompleteEvent();
		}

		protected override void OnEventComplete()
		{
			if (_ruins != null && GodotObject.IsInstanceValid(_ruins))
				_ruins.Built -= OnRuinsBuilt;
			_eventMarker?.Hide();
		}

		protected override void OnEventFail()
		{
			if (_ruins != null && GodotObject.IsInstanceValid(_ruins))
				_ruins.Built -= OnRuinsBuilt;
			_eventMarker?.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();

			if (State != EventState.Completed && _ruins != null && GodotObject.IsInstanceValid(_ruins))
				_ruins.QueueFree();
		}
	}
}
