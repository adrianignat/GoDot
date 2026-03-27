using dTopDownShooter.Scripts.Characters.Allies;
using dTopDownShooter.Scripts.UI;
using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public enum EscortPhase
	{
		WaitingForTrigger,
		TravelingToMonastery,
		Escorting,
		Completed,
		Failed
	}

	public partial class EscortEvent : GameEvent
	{
		private const string MonkScenePath = "res://Entities/Characters/monk.tscn";
		private const float MinDistanceBetweenLocations = 800f;
		private const float StageCount = 3f;

		public override string EventId => "escort_monk";
		public override string DisplayName => "Escort the Monk";
		public override ushort CompletionRewardGold => 20;

		private EscortPhase _phase = EscortPhase.WaitingForTrigger;
		private float _triggerTimer;
		private Vector2 _monasteryPosition;
		private Vector2 _monastaryRuinsPosition;
		private Node2D _monastery;
		private Monk _monk;
		private EventMarker _eventMarker;
		protected override EventMarker QuestMarker => _eventMarker;
		private MonastaryRuins _targetMonastary;
		private MapGenerator _mapGenerator;

		public override void _Ready()
		{
			_triggerTimer = GameConstants.EscortEventTriggerDelay;
		}

		protected override void OnEventStart()
		{
			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");

			_eventMarker = new EventMarker();
			_eventMarker.Name = "EscortEventMarker";
			Game.Instance.AddChild(_eventMarker);
			TriggerEvent();
		}

		protected override void OnEventUpdate(double delta)
		{
			switch (_phase)
			{
				case EscortPhase.WaitingForTrigger:
					UpdateWaitingPhase(delta);
					break;
				case EscortPhase.TravelingToMonastery:
					UpdateTravelingToMonasteryPhase();
					break;
				case EscortPhase.Escorting:
					UpdateEscortingPhase();
					break;
			}
		}

		private void UpdateWaitingPhase(double delta)
		{
			_triggerTimer -= (float)delta;
			if (_triggerTimer <= 0)
				TriggerEvent();
		}

		private void TriggerEvent()
		{
			if (_mapGenerator == null)
			{
				GD.PrintErr("[EscortEvent] MapGenerator not found");
				FailEvent();
				return;
			}

			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			var markerPosition = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, MinDistanceBetweenLocations);

			if (!markerPosition.HasValue)
			{
				GD.PrintErr("[EscortEvent] No spawn markers available for monastery");
				FailEvent();
				return;
			}

			_monasteryPosition = markerPosition.Value;
			_monastery = _mapGenerator.SpawnMonasteryAt(_monasteryPosition);
			if (_monastery == null)
			{
				GD.PrintErr("[EscortEvent] Failed to spawn monastery");
				FailEvent();
				return;
			}

			_phase = EscortPhase.TravelingToMonastery;
			SetObjective("Find the monastery");
			_eventMarker.Configure("Monastery", Colors.Cyan);
			_eventMarker.SetTarget(_monasteryPosition);
			_eventMarker.SetStatus("Stage 1/3: Find the monk");
			_eventMarker.SetProgress(1f, StageCount);
			_eventMarker.Show();
		}

		private Vector2 PickMonastaryRuinsPosition()
		{
			var markerPosition = _mapGenerator.GetSpawnMarkerAwayFrom(_monasteryPosition, MinDistanceBetweenLocations);
			if (markerPosition.HasValue)
				return markerPosition.Value;

			var playableArea = _mapGenerator.GetPlayableAreaBounds();
			Vector2 mapCenter = _mapGenerator.GetPlayerSpawnPosition();
			Vector2 directionFromMonastery = (mapCenter - _monasteryPosition).Normalized();
			Vector2 candidatePosition = mapCenter + directionFromMonastery * MinDistanceBetweenLocations;

			float margin = 200f;
			candidatePosition.X = Mathf.Clamp(candidatePosition.X, playableArea.Position.X + margin, playableArea.Position.X + playableArea.Size.X - margin);
			candidatePosition.Y = Mathf.Clamp(candidatePosition.Y, playableArea.Position.Y + margin, playableArea.Position.Y + playableArea.Size.Y - margin);
			return candidatePosition;
		}

		private void UpdateTravelingToMonasteryPhase()
		{
			var player = Game.Instance.Player;
			if (player == null)
				return;

			if (_monastery == null || !IsInstanceValid(_monastery))
			{
				FailEvent();
				return;
			}

			if (player.GlobalPosition.DistanceTo(_monasteryPosition) <= GameConstants.EventInteractionRadius)
				SpawnMonkAndMonastaryRuins();
		}

		private void SpawnMonkAndMonastaryRuins()
		{
			var monkScene = GD.Load<PackedScene>(MonkScenePath);
			if (monkScene == null)
			{
				FailEvent();
				return;
			}

			_monk = monkScene.Instantiate<Monk>();
			_monk.GlobalPosition = _monasteryPosition + new Vector2(50, 30);
			_mapGenerator.AddChild(_monk);
			_monk.StartFollowing();

			_monastaryRuinsPosition = PickMonastaryRuinsPosition();
			_targetMonastary = _mapGenerator.SpawnMonastaryRuinsAt(_monastaryRuinsPosition);
			if (_targetMonastary == null)
			{
				FailEvent();
				return;
			}

			_phase = EscortPhase.Escorting;
			SetObjective("Escort the monk to the monastary");
			_eventMarker.Configure("Monastary", Colors.Gold);
			_eventMarker.SetTarget(_monastaryRuinsPosition);
			_eventMarker.SetStatus("Stage 2/3: Escort the monk");
			_eventMarker.SetProgress(2f, StageCount);
		}

		private void UpdateEscortingPhase()
		{
			if (_monk == null || !IsInstanceValid(_monk))
			{
				FailEvent();
				return;
			}

			if (_targetMonastary == null || !IsInstanceValid(_targetMonastary))
			{
				FailEvent();
				return;
			}

			var player = Game.Instance.Player;
			if (player == null)
				return;

			Vector2 monastaryPos = _targetMonastary.GlobalPosition;
			float playerDistance = player.GlobalPosition.DistanceTo(monastaryPos);
			float monkDistance = _monk.GlobalPosition.DistanceTo(monastaryPos);

			if (playerDistance <= GameConstants.EventInteractionRadius && monkDistance <= GameConstants.EventInteractionRadius * 1.5f)
				OnReachedDestination();
		}

		private void OnReachedDestination()
		{
			if (_monk != null && IsInstanceValid(_monk))
				_monk.StopFollowing();

			_phase = EscortPhase.Completed;
			_eventMarker?.SetStatus("Stage 3/3: Escort complete");
			_eventMarker?.SetProgress(StageCount, StageCount);
			CompleteEvent();
		}

		protected override void OnEventComplete()
		{
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
				_eventMarker.Hide();
		}

		protected override void OnEventFail()
		{
			_phase = EscortPhase.Failed;
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
				_eventMarker.Hide();
		}

		public override void CleanupEventEntities()
		{
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
			{
				_eventMarker.QueueFree();
				_eventMarker = null;
			}

			if (_monk != null && IsInstanceValid(_monk))
			{
				_monk.QueueFree();
				_monk = null;
			}

			if (_monastery != null && IsInstanceValid(_monastery))
			{
				_monastery.QueueFree();
				_monastery = null;
			}

			_targetMonastary = null;
		}
	}
}
