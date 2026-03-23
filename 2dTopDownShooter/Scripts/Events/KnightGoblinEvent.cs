using dTopDownShooter.Scripts.Characters.Allies;
using dTopDownShooter.Scripts.UI;
using Godot;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts.Events
{
	public enum KnightQuestPhase
	{
		FindKnight,
		TravelToArena,
		BossFight,
		Completed,
		Failed
	}

	public partial class KnightGoblinEvent : GameEvent
	{
		private const string KnightScenePath = "res://Entities/Characters/knight_ally.tscn";
		private const string BossScenePath = "res://Entities/Characters/Enemies/ogre_boss.tscn";
		private const string GoblinScenePath = "res://Entities/Characters/Enemies/enemy.tscn";
		private const float FindDistance = 800f;
		private const float ArenaArrivalRadius = 140f;
		private const float KnightPickupRadius = 110f;

		public override string EventId => "help_knight";
		public override string DisplayName => "Help the Knight Defeat the Ogre";

		private MapGenerator _mapGenerator;
		private EventMarker _eventMarker;
		private KnightAlly _knight;
		private Enemy _boss;
		private readonly List<Enemy> _trapAdds = new();
		private KnightQuestPhase _phase;
		private Vector2 _knightPosition;
		private Vector2 _arenaPosition;

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");
			_eventMarker = new EventMarker();
			_eventMarker.Name = "KnightQuestMarker";
			Game.Instance.AddChild(_eventMarker);

			if (_mapGenerator == null)
			{
				FailEvent();
				return;
			}

			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			var knightMarker = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, FindDistance);
			if (!knightMarker.HasValue)
			{
				FailEvent();
				return;
			}

			_knightPosition = knightMarker.Value;
			_arenaPosition = _mapGenerator.GetPlayerSpawnPosition();
			SpawnKnight();
			Game.Instance.EnemyKilled += OnEnemyKilled;
			_phase = KnightQuestPhase.FindKnight;
			SetObjective("Find the knight");
			_eventMarker.Configure("Knight", Colors.OrangeRed);
			_eventMarker.SetTarget(_knight);
			_eventMarker.Show();
		}

		protected override void OnEventUpdate(double delta)
		{
			var player = Game.Instance.Player;
			if (player == null)
				return;

			switch (_phase)
			{
				case KnightQuestPhase.FindKnight:
					if (_knight == null || !GodotObject.IsInstanceValid(_knight))
					{
						FailEvent();
						return;
					}

					if (player.GlobalPosition.DistanceTo(_knight.GlobalPosition) <= KnightPickupRadius)
					{
						_knight.StartFollowing();
						_phase = KnightQuestPhase.TravelToArena;
						SetObjective("Lead the knight to the center of the arena");
						_eventMarker.Configure("Arena", Colors.Gold);
						_eventMarker.SetTarget(_arenaPosition);
					}
					break;

				case KnightQuestPhase.TravelToArena:
					if (_knight == null || !GodotObject.IsInstanceValid(_knight))
					{
						FailEvent();
						return;
					}

					if (player.GlobalPosition.DistanceTo(_arenaPosition) <= ArenaArrivalRadius &&
						_knight.GlobalPosition.DistanceTo(_arenaPosition) <= ArenaArrivalRadius * 1.4f)
					{
						TriggerBossFight();
					}
					break;

				case KnightQuestPhase.BossFight:
					if (_boss == null || !GodotObject.IsInstanceValid(_boss))
					{
						FailEvent();
						return;
					}
					_eventMarker.SetTarget(_boss);
					break;
			}
		}

		private void SpawnKnight()
		{
			var knightScene = GD.Load<PackedScene>(KnightScenePath);
			if (knightScene == null)
			{
				FailEvent();
				return;
			}

			_knight = knightScene.Instantiate<KnightAlly>();
			_knight.GlobalPosition = _knightPosition;
			_mapGenerator.AddChild(_knight);
		}

		private void TriggerBossFight()
		{
			_phase = KnightQuestPhase.BossFight;
			SetObjective("Defeat the ogre");
			_eventMarker.Configure("Ogre", Colors.Red);

			var bossScene = GD.Load<PackedScene>(BossScenePath);
			var goblinScene = GD.Load<PackedScene>(GoblinScenePath);
			if (bossScene == null || goblinScene == null)
			{
				FailEvent();
				return;
			}

			_boss = bossScene.Instantiate<Enemy>();
			_boss.Tier = EnemyTier.Yellow;
			_boss.GlobalPosition = _arenaPosition + new Vector2(0, -140);
			_mapGenerator.AddChild(_boss);

			Vector2[] addOffsets =
			{
				new(-200, 0),
				new(200, 0),
				new(0, 180),
				new(0, -260)
			};

			foreach (var offset in addOffsets)
			{
				var add = goblinScene.Instantiate<Enemy>();
				add.Tier = EnemyTier.Red;
				add.GlobalPosition = _arenaPosition + offset;
				_mapGenerator.AddChild(add);
				_trapAdds.Add(add);
			}
		}

		private void OnEnemyKilled(Enemy enemy)
		{
			if (_boss != null && enemy == _boss)
			{
				_phase = KnightQuestPhase.Completed;
				CompleteEvent();
			}
		}

		protected override void OnEventComplete()
		{
			Game.Instance.EnemyKilled -= OnEnemyKilled;
			_eventMarker?.Hide();
			if (_knight != null && GodotObject.IsInstanceValid(_knight))
				_knight.StopFollowing();
		}

		protected override void OnEventFail()
		{
			_phase = KnightQuestPhase.Failed;
			Game.Instance.EnemyKilled -= OnEnemyKilled;
			_eventMarker?.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && GodotObject.IsInstanceValid(_eventMarker))
				_eventMarker.QueueFree();
			if (_knight != null && GodotObject.IsInstanceValid(_knight))
				_knight.QueueFree();
			if (_boss != null && GodotObject.IsInstanceValid(_boss))
				_boss.QueueFree();
			foreach (var add in _trapAdds)
			{
				if (add != null && GodotObject.IsInstanceValid(add))
					add.QueueFree();
			}
		}
	}
}
