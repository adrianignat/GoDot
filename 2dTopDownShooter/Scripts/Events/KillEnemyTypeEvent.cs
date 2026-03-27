using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.UI;
using Godot;
using System;

namespace dTopDownShooter.Scripts.Events
{
	public partial class KillEnemyTypeEvent : GameEvent
	{
		private readonly (Type Type, string Label, int Count, Color Color)[] _targets =
		{
			(typeof(TorchGoblin), "Torch Goblins", 16, Colors.OrangeRed),
			(typeof(TntGoblin), "TNT Goblins", 8, Colors.Goldenrod),
			(typeof(Shaman), "Shamans", 6, Colors.DeepSkyBlue)
		};

		public override string EventId => "kill_enemy_type";
		public override string DisplayName => "Cull a Specific Enemy Type";
		public override ushort CompletionRewardGold => 15;

		private Type _targetType;
		private string _targetLabel;
		private int _requiredKills;
		private int _currentKills;
		private Color _markerColor;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;

		protected override void OnEventStart()
		{
			var choice = _targets[GD.RandRange(0, _targets.Length - 1)];
			_targetType = choice.Type;
			_targetLabel = choice.Label;
			_requiredKills = choice.Count;
			_markerColor = choice.Color;
			_currentKills = 0;

			_eventMarker = new EventMarker();
			_eventMarker.Name = "CullEnemyTypeMarker";
			_eventMarker.SetNavigationVisible(false);
			Game.Instance.AddChild(_eventMarker);

			Game.Instance.EnemyKilled += OnEnemyKilled;
			UpdateObjectiveUI();
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
		}

		private void OnEnemyKilled(Enemy enemy)
		{
			if (!_targetType.IsInstanceOfType(enemy))
				return;

			_currentKills++;
			if (_currentKills >= _requiredKills)
			{
				_eventMarker?.SetProgress(_requiredKills, _requiredKills);
				CompleteEvent();
				return;
			}

			UpdateObjectiveUI();
		}

		private void UpdateObjectiveUI()
		{
			SetObjective($"Kill {_targetLabel}: {_currentKills}/{_requiredKills}");
			if (_eventMarker == null)
				return;

			_eventMarker.Configure(_targetLabel, _markerColor);
			_eventMarker.SetStatus($"Killed {_currentKills}/{_requiredKills}");
			_eventMarker.SetProgress(_currentKills, _requiredKills);
		}

		protected override void OnEventComplete()
		{
			Game.Instance.EnemyKilled -= OnEnemyKilled;
			_eventMarker?.Hide();
		}

		protected override void OnEventFail()
		{
			Game.Instance.EnemyKilled -= OnEnemyKilled;
			_eventMarker?.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();
		}
	}
}
