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

	/// <summary>
	/// Escort quest: Go to the monastery to find a monk, then escort them to the monastary ruins.
	/// The monastery is spawned at a map marker when the event triggers.
	/// The monastary ruins is spawned only after finding the monk.
	/// On completion, the monastary ruins becomes a shelter.
	/// </summary>
	public partial class EscortEvent : GameEvent
	{
		private const string MonkScenePath = "res://Entities/Characters/monk.tscn";
		private const float MinDistanceBetweenLocations = 800f;

		public override string EventId => "escort_monk";
		public override string DisplayName => "Escort the Monk";

		private EscortPhase _phase = EscortPhase.WaitingForTrigger;
		private float _triggerTimer;
		private Vector2 _monasteryPosition;
		private Vector2 _monastaryRuinsPosition;
		private Node2D _monastery;
		private Monk _monk;
		private EventMarker _eventMarker;
		private MonastaryRuins _targetMonastary;
		private MapGenerator _mapGenerator;

		public override void _Ready()
		{
			_triggerTimer = GameConstants.EscortEventTriggerDelay;
		}

		protected override void OnEventStart()
		{
			_phase = EscortPhase.WaitingForTrigger;
			_triggerTimer = GameConstants.EscortEventTriggerDelay;

			_mapGenerator = Game.Instance.MainWindow.GetNodeOrNull<MapGenerator>("MapGenerator");

			// Create event marker (hidden until we need it)
			_eventMarker = new EventMarker();
			_eventMarker.Name = "EscortEventMarker";
			Game.Instance.AddChild(_eventMarker);

			GD.Print($"[EscortEvent] Event started, waiting {_triggerTimer}s to trigger");
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
			{
				TriggerEvent();
			}
		}

		private void TriggerEvent()
		{
			if (_mapGenerator == null)
			{
				GD.PrintErr("[EscortEvent] MapGenerator not found");
				FailEvent();
				return;
			}

			// Pick a spawn marker position for the monastery (far from player)
			var playerPos = Game.Instance.Player?.GlobalPosition ?? _mapGenerator.GetPlayerSpawnPosition();
			var markerPosition = _mapGenerator.GetSpawnMarkerAwayFrom(playerPos, MinDistanceBetweenLocations);

			if (!markerPosition.HasValue)
			{
				GD.PrintErr("[EscortEvent] No spawn markers available for monastery");
				FailEvent();
				return;
			}

			_monasteryPosition = markerPosition.Value;

			// Spawn monastery at the marker position
			_monastery = _mapGenerator.SpawnMonasteryAt(_monasteryPosition);
			if (_monastery == null)
			{
				GD.PrintErr("[EscortEvent] Failed to spawn monastery");
				FailEvent();
				return;
			}

			_phase = EscortPhase.TravelingToMonastery;
			SetObjective("Find the monastery");

			// Show marker pointing to monastery
			_eventMarker.Configure("Monastery", Colors.Cyan);
			_eventMarker.SetTarget(_monastery);
			_eventMarker.Show();

			GD.Print($"[EscortEvent] Triggered! Go to {_monasteryPosition} to find the monastery");
		}


		private Vector2 PickMonastaryRuinsPosition()
		{
			// Try to get a spawn marker far from the monastery
			var markerPosition = _mapGenerator.GetSpawnMarkerAwayFrom(_monasteryPosition, MinDistanceBetweenLocations);
			if (markerPosition.HasValue)
			{
				GD.Print($"[EscortEvent] Monastary ruins position from marker: {markerPosition.Value}, distance from monastery: {markerPosition.Value.DistanceTo(_monasteryPosition)}");
				return markerPosition.Value;
			}

			// Fallback: calculate position manually if no suitable marker found
			var playableArea = _mapGenerator.GetPlayableAreaBounds();
			Vector2 mapCenter = _mapGenerator.GetPlayerSpawnPosition();

			// Try to place monastary ruins on opposite side of map from monastery
			Vector2 directionFromMonastery = (mapCenter - _monasteryPosition).Normalized();
			Vector2 candidatePosition = mapCenter + directionFromMonastery * MinDistanceBetweenLocations;

			// Clamp to playable area with some margin
			float margin = 200f;
			candidatePosition.X = Mathf.Clamp(candidatePosition.X,
				playableArea.Position.X + margin,
				playableArea.Position.X + playableArea.Size.X - margin);
			candidatePosition.Y = Mathf.Clamp(candidatePosition.Y,
				playableArea.Position.Y + margin,
				playableArea.Position.Y + playableArea.Size.Y - margin);

			// Ensure minimum distance from monastery
			float distanceFromMonastery = candidatePosition.DistanceTo(_monasteryPosition);
			if (distanceFromMonastery < MinDistanceBetweenLocations)
			{
				// Try other directions
				Vector2[] directions = new Vector2[]
				{
					new Vector2(1, 0),
					new Vector2(-1, 0),
					new Vector2(0, 1),
					new Vector2(0, -1),
					new Vector2(1, 1).Normalized(),
					new Vector2(-1, -1).Normalized(),
					new Vector2(1, -1).Normalized(),
					new Vector2(-1, 1).Normalized()
				};

				float bestDist = 0;
				Vector2 bestPos = candidatePosition;

				foreach (var dir in directions)
				{
					Vector2 testPos = _monasteryPosition + dir * MinDistanceBetweenLocations;
					testPos.X = Mathf.Clamp(testPos.X,
						playableArea.Position.X + margin,
						playableArea.Position.X + playableArea.Size.X - margin);
					testPos.Y = Mathf.Clamp(testPos.Y,
						playableArea.Position.Y + margin,
						playableArea.Position.Y + playableArea.Size.Y - margin);

					float dist = testPos.DistanceTo(_monasteryPosition);
					if (dist > bestDist)
					{
						bestDist = dist;
						bestPos = testPos;
					}
				}
				candidatePosition = bestPos;
			}

			GD.Print($"[EscortEvent] Monastary ruins position (fallback): {candidatePosition}, distance from monastery: {candidatePosition.DistanceTo(_monasteryPosition)}");
			return candidatePosition;
		}

		private void UpdateTravelingToMonasteryPhase()
		{
			var player = Game.Instance.Player;
			if (player == null)
				return;

			// Check if monastery still exists
			if (_monastery == null || !IsInstanceValid(_monastery))
			{
				GD.PrintErr("[EscortEvent] Monastery was destroyed - event failed");
				FailEvent();
				return;
			}

			float distance = player.GlobalPosition.DistanceTo(_monasteryPosition);
			if (distance <= GameConstants.EventInteractionRadius)
			{
				SpawnMonkAndMonastaryRuins();
			}
		}

		private void SpawnMonkAndMonastaryRuins()
		{
			// Spawn monk at the monastery position
			var monkScene = GD.Load<PackedScene>(MonkScenePath);
			if (monkScene == null)
			{
				GD.PrintErr("[EscortEvent] Failed to load monk scene");
				FailEvent();
				return;
			}

			_monk = monkScene.Instantiate<Monk>();
			// Spawn monk slightly offset from monastery so they don't overlap
			_monk.GlobalPosition = _monasteryPosition + new Vector2(50, 30);
			_mapGenerator.AddChild(_monk);
			_monk.StartFollowing();

			GD.Print("[EscortEvent] Monk spawned at monastery and now following player");

			// Pick monastary ruins position far from monastery
			_monastaryRuinsPosition = PickMonastaryRuinsPosition();
			_targetMonastary = _mapGenerator.SpawnMonastaryRuinsAt(_monastaryRuinsPosition);

			if (_targetMonastary == null)
			{
				GD.PrintErr("[EscortEvent] Failed to spawn monastary ruins");
				FailEvent();
				return;
			}

			// Update phase and marker
			_phase = EscortPhase.Escorting;
			SetObjective("Escort the monk to the monastary");

			// Point marker to the monastary ruins
			_eventMarker.Configure("Monastary", Colors.Gold);
			_eventMarker.SetTarget(_targetMonastary);

			GD.Print($"[EscortEvent] Monastary ruins spawned at {_monastaryRuinsPosition}");
		}

		private void UpdateEscortingPhase()
		{
			// Check if monk is still alive
			if (_monk == null || !IsInstanceValid(_monk))
			{
				GD.Print("[EscortEvent] Monk was killed - event failed");
				FailEvent();
				return;
			}

			// Check if monastary still exists
			if (_targetMonastary == null || !IsInstanceValid(_targetMonastary))
			{
				GD.PrintErr("[EscortEvent] Monastary was destroyed - event failed");
				FailEvent();
				return;
			}

			// Check if player and monk reached the destination
			var player = Game.Instance.Player;
			if (player == null)
				return;

			Vector2 monastaryPos = _targetMonastary.GlobalPosition;
			float playerDistance = player.GlobalPosition.DistanceTo(monastaryPos);
			float monkDistance = _monk.GlobalPosition.DistanceTo(monastaryPos);

			// Both player and monk need to be close
			if (playerDistance <= GameConstants.EventInteractionRadius &&
				monkDistance <= GameConstants.EventInteractionRadius * 1.5f)
			{
				OnReachedDestination();
			}
		}

		private void OnReachedDestination()
		{
			GD.Print("[EscortEvent] Reached destination - completing event");

			// Stop monk from following
			if (_monk != null && IsInstanceValid(_monk))
			{
				_monk.StopFollowing();
			}

			// The monastary will auto-build when player stays in range
			GD.Print("[EscortEvent] Monastary ruins can now be built into a shelter");

			_phase = EscortPhase.Completed;
			CompleteEvent();
		}

		protected override void OnEventComplete()
		{
			// Hide marker
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
			{
				_eventMarker.Hide();
			}

			GD.Print("[EscortEvent] Event completed successfully!");
		}

		protected override void OnEventFail()
		{
			_phase = EscortPhase.Failed;

			// Hide marker
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
			{
				_eventMarker.Hide();
			}

			GD.Print("[EscortEvent] Event failed!");
		}

		public override void CleanupEventEntities()
		{
			// Cleanup marker
			if (_eventMarker != null && IsInstanceValid(_eventMarker))
			{
				_eventMarker.QueueFree();
				_eventMarker = null;
			}

			// Cleanup monk
			if (_monk != null && IsInstanceValid(_monk))
			{
				_monk.QueueFree();
				_monk = null;
			}

			// Cleanup monastery (the start building)
			if (_monastery != null && IsInstanceValid(_monastery))
			{
				_monastery.QueueFree();
				_monastery = null;
			}

			// Note: We don't cleanup monastary ruins as it becomes a permanent building
			_targetMonastary = null;

			GD.Print("[EscortEvent] Cleaned up event entities");
		}
	}
}
