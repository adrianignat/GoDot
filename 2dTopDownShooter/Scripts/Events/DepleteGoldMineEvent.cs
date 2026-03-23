using dTopDownShooter.Scripts.UI;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class DepleteGoldMineEvent : GameEvent
	{
		private const float MinDistance = 700f;

		public override string EventId => "deplete_gold_mine";
		public override string DisplayName => "Deplete the Gold Mine";

		private MapGenerator _mapGenerator;
		private GoldmineRuins _goldmine;
		private EventMarker _eventMarker;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "GoldMineMarker";
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

			_goldmine = _mapGenerator.SpawnGoldmineRuinsAt(marker.Value);
			if (_goldmine == null)
			{
				FailEvent();
				return;
			}

			_goldmine.Built += OnMineBuilt;
			_goldmine.GoldExtracted += OnGoldExtracted;
			_goldmine.Depleted += OnMineDepleted;

			SetObjective("Travel to the gold mine and activate it");
			_eventMarker.Configure("Gold Mine", Colors.Yellow);
			_eventMarker.SetTarget(_goldmine);
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			if (_goldmine == null || !GodotObject.IsInstanceValid(_goldmine))
				FailEvent();
		}

		private void OnMineBuilt(GoldmineRuins mine)
		{
			SetObjective($"Extract the remaining gold ({mine.RemainingGold} left)");
		}

		private void OnGoldExtracted(GoldmineRuins mine, int amountExtracted, int remainingAmount)
		{
			SetObjective($"Extract the remaining gold ({remainingAmount} left)");
		}

		private void OnMineDepleted(GoldmineRuins mine)
		{
			CompleteEvent();
		}

		protected override void OnEventComplete()
		{
			DisconnectMineSignals();
			_eventMarker?.Hide();
		}

		protected override void OnEventFail()
		{
			DisconnectMineSignals();
			_eventMarker?.Hide();
		}

		private void DisconnectMineSignals()
		{
			if (_goldmine == null || !GodotObject.IsInstanceValid(_goldmine))
				return;

			_goldmine.Built -= OnMineBuilt;
			_goldmine.GoldExtracted -= OnGoldExtracted;
			_goldmine.Depleted -= OnMineDepleted;
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();

			if (State != EventState.Completed && _goldmine != null && GodotObject.IsInstanceValid(_goldmine))
				_goldmine.QueueFree();
		}
	}
}
