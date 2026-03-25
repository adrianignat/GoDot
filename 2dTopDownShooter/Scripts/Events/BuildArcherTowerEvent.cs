using dTopDownShooter.Scripts.UI;
using Godot;
using System.Linq;

namespace dTopDownShooter.Scripts.Events
{
	public partial class BuildArcherTowerEvent : GameEvent
	{
		private const float MinDistance = 700f;

		public override string EventId => "build_archer_tower";
		public override string DisplayName => "Build the Archer Tower";
		public override ushort CompletionRewardGold => 15;

		private MapGenerator _mapGenerator;
		private TowerRuins _ruins;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;

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
			_ruins = _mapGenerator.GetTowerRuins()
				.Where(ruins => ruins != null && GodotObject.IsInstanceValid(ruins) && !ruins.IsBuilt)
				.OrderByDescending(ruins => ruins.GlobalPosition.DistanceTo(playerPos))
				.FirstOrDefault(ruins => ruins.GlobalPosition.DistanceTo(playerPos) >= MinDistance)
				?? _mapGenerator.GetTowerRuins()
					.Where(ruins => ruins != null && GodotObject.IsInstanceValid(ruins) && !ruins.IsBuilt)
					.OrderByDescending(ruins => ruins.GlobalPosition.DistanceTo(playerPos))
					.FirstOrDefault();

			if (_ruins == null)
			{
				FailEvent();
				return;
			}

			_ruins.BuildProgressChanged += OnBuildProgressChanged;
			_ruins.Built += OnRuinsBuilt;
			SetObjective("Travel to the tower ruins and rebuild the archer tower");
			_eventMarker.Configure("Archer Tower", Colors.Gold);
			_eventMarker.SetTarget(_ruins);
			_eventMarker.SetStatus($"Rebuild 0/{Mathf.CeilToInt(_ruins.BuildTimeRequired)}");
			_eventMarker.SetProgress(0f, _ruins.BuildTimeRequired);
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			if (_ruins == null || !GodotObject.IsInstanceValid(_ruins))
				FailEvent();
		}

		private void OnBuildProgressChanged(TowerRuins ruins, float currentProgress, float requiredProgress)
		{
			int currentStep = Mathf.Clamp(Mathf.FloorToInt(currentProgress), 0, Mathf.CeilToInt(requiredProgress));
			int requiredSteps = Mathf.CeilToInt(requiredProgress);
			SetObjective($"Build the archer tower {currentStep}/{requiredSteps}");
			_eventMarker?.SetStatus($"Rebuild {currentStep}/{requiredSteps}");
			_eventMarker?.SetProgress(currentProgress, requiredProgress);
		}

		private void OnRuinsBuilt(TowerRuins ruins)
		{
			_eventMarker?.SetStatus("Tower completed");
			_eventMarker?.SetProgress(ruins.BuildTimeRequired, ruins.BuildTimeRequired);
			CompleteEvent();
		}

		protected override void OnEventComplete()
		{
			if (_ruins != null && GodotObject.IsInstanceValid(_ruins))
			{
				_ruins.BuildProgressChanged -= OnBuildProgressChanged;
				_ruins.Built -= OnRuinsBuilt;
			}
			_eventMarker?.Hide();
		}

		protected override void OnEventFail()
		{
			if (_ruins != null && GodotObject.IsInstanceValid(_ruins))
			{
				_ruins.BuildProgressChanged -= OnBuildProgressChanged;
				_ruins.Built -= OnRuinsBuilt;
			}
			_eventMarker?.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();
		}
	}
}
