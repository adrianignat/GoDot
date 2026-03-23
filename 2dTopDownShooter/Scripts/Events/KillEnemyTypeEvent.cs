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
			(typeof(TorchGoblin), "Torch Goblins", 8, Colors.OrangeRed),
			(typeof(TntGoblin), "TNT Goblins", 3, Colors.Goldenrod),
			(typeof(Shaman), "Shamans", 2, Colors.DeepSkyBlue)
		};

		public override string EventId => "kill_enemy_type";
		public override string DisplayName => "Cull a Specific Enemy Type";

		private Type _targetType;
		private string _targetLabel;
		private int _requiredKills;
		private int _currentKills;
		private Color _markerColor;
		private EventMarker _eventMarker;

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
			Game.Instance.AddChild(_eventMarker);

			Game.Instance.EnemyKilled += OnEnemyKilled;
			UpdateObjectiveUI();
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			if (_eventMarker == null)
				return;

			var targetEnemy = FindNearestMatchingEnemy();
			if (targetEnemy != null)
				_eventMarker.SetTarget(targetEnemy);
			else if (Game.Instance.Player != null)
				_eventMarker.SetTarget(Game.Instance.Player.GlobalPosition + new Vector2(0, -200));
		}

		private Enemy FindNearestMatchingEnemy()
		{
			var player = Game.Instance.Player;
			if (player == null)
				return null;

			Enemy nearest = null;
			float bestDistance = float.MaxValue;
			foreach (Node node in GetTree().GetNodesInGroup(GameConstants.EnemiesGroup))
			{
				if (node is not Enemy enemy || !_targetType.IsInstanceOfType(enemy))
					continue;

				float distance = player.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					nearest = enemy;
				}
			}

			return nearest;
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

			UpdateObjectiveUI();
		}

		private void UpdateObjectiveUI()
		{
			SetObjective($"Kill {_targetLabel}: {_currentKills}/{_requiredKills}");
			if (_eventMarker == null)
				return;

			_eventMarker.Configure(_targetLabel, _markerColor);
			_eventMarker.SetStatus($"Killed {_currentKills}/{_requiredKills}");
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
