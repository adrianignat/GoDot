using Godot;
using System;

namespace dTopDownShooter.Scripts.Events
{
	public partial class KillEnemyTypeEvent : GameEvent
	{
		private readonly (Type Type, string Label, int Count)[] _targets =
		{
			(typeof(TorchGoblin), "Torch Goblins", 8),
			(typeof(TntGoblin), "TNT Goblins", 3),
			(typeof(Shaman), "Shamans", 2)
		};

		public override string EventId => "kill_enemy_type";
		public override string DisplayName => "Cull a Specific Enemy Type";

		private Type _targetType;
		private string _targetLabel;
		private int _requiredKills;
		private int _currentKills;

		protected override void OnEventStart()
		{
			var choice = _targets[GD.RandRange(0, _targets.Length - 1)];
			_targetType = choice.Type;
			_targetLabel = choice.Label;
			_requiredKills = choice.Count;
			_currentKills = 0;

			Game.Instance.EnemyKilled += OnEnemyKilled;
			SetObjective($"Kill {_requiredKills} {_targetLabel}");
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
				CompleteEvent();
				return;
			}

			SetObjective($"Kill {_requiredKills - _currentKills} more {_targetLabel}");
		}

		protected override void OnEventComplete()
		{
			Game.Instance.EnemyKilled -= OnEnemyKilled;
		}

		protected override void OnEventFail()
		{
			Game.Instance.EnemyKilled -= OnEnemyKilled;
		}
	}
}
