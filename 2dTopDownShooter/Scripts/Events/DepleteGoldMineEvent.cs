using dTopDownShooter.Scripts.UI;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class DepleteGoldMineEvent : GameEvent
	{
		private const float MinDistance = 700f;

		public override string EventId => "deplete_gold_mine";
		public override string DisplayName => "Deplete the Gold Mine";
		public override ushort CompletionRewardGold => 20;

		private MapGenerator _mapGenerator;
		private GoldmineRuins _goldmine;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;

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

			_goldmine.BuildProgressChanged += OnBuildProgressChanged;
			_goldmine.Built += OnMineBuilt;
			_goldmine.GoldExtracted += OnGoldExtracted;
			_goldmine.Depleted += OnMineDepleted;

			SetObjective("Travel to the gold mine and rebuild it");
			_eventMarker.Configure("Gold Mine", Colors.Yellow);
			_eventMarker.SetTarget(_goldmine);
			_eventMarker.SetStatus("Find and rebuild the mine");
			_eventMarker.SetProgress(0f, _goldmine.BuildTimeRequired + _goldmine.TotalGold);
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			if (_goldmine == null || !GodotObject.IsInstanceValid(_goldmine))
				FailEvent();
		}

		private void OnBuildProgressChanged(GoldmineRuins mine, float currentProgress, float requiredProgress)
		{
			SetObjective($"Rebuild the mine {Mathf.FloorToInt(currentProgress)}/{Mathf.CeilToInt(requiredProgress)}");
			_eventMarker?.SetStatus($"Rebuild {Mathf.FloorToInt(currentProgress)}/{Mathf.CeilToInt(requiredProgress)}");
			_eventMarker?.SetProgress(currentProgress, mine.BuildTimeRequired + mine.TotalGold);
		}

		private void OnMineBuilt(GoldmineRuins mine)
		{
			int extracted = mine.TotalGold - mine.RemainingGold;
			SetObjective($"Extract gold {extracted}/{mine.TotalGold}");
			_eventMarker?.SetStatus($"Extracted {extracted}/{mine.TotalGold}");
			_eventMarker?.SetProgress(mine.BuildTimeRequired + extracted, mine.BuildTimeRequired + mine.TotalGold);
		}

		private void OnGoldExtracted(GoldmineRuins mine, int amountExtracted, int remainingAmount)
		{
			int extracted = mine.TotalGold - remainingAmount;
			SetObjective($"Extract gold {extracted}/{mine.TotalGold}");
			_eventMarker?.SetStatus($"Extracted {extracted}/{mine.TotalGold}");
			_eventMarker?.SetProgress(mine.BuildTimeRequired + extracted, mine.BuildTimeRequired + mine.TotalGold);
		}

		private void OnMineDepleted(GoldmineRuins mine)
		{
			_eventMarker?.SetStatus("Mine depleted");
			_eventMarker?.SetProgress(mine.BuildTimeRequired + mine.TotalGold, mine.BuildTimeRequired + mine.TotalGold);
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

			_goldmine.BuildProgressChanged -= OnBuildProgressChanged;
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
