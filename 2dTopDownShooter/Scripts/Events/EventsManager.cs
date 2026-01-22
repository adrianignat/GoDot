using Godot;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts.Events
{
	/// <summary>
	/// Central manager for all game events. Handles event lifecycle and cleanup.
	/// </summary>
	public partial class EventsManager : Node
	{
		private Dictionary<string, GameEvent> _activeEvents = new();
		private List<GameEvent> _pendingEvents = new();

		public override void _Ready()
		{
			// Connect to day/night signals
			Game.Instance.DayStarted += OnDayStarted;
			Game.Instance.NightStarted += OnNightStarted;
			Game.Instance.DayCompleted += OnDayCompleted;

			// Register default events
			RegisterEscortEvent();
		}

		private void RegisterEscortEvent()
		{
			var escortEvent = new EscortEvent();
			escortEvent.Name = "EscortEvent";
			AddChild(escortEvent);
			_pendingEvents.Add(escortEvent);
		}

		public override void _Process(double delta)
		{
			// Update all active events
			foreach (var eventEntry in _activeEvents.Values)
			{
				eventEntry.UpdateEvent(delta);
			}
		}

		/// <summary>
		/// Register an event to be triggered.
		/// </summary>
		public void RegisterEvent(GameEvent gameEvent)
		{
			if (!IsInstanceValid(gameEvent))
			{
				GD.PrintErr("[EventsManager] Attempted to register invalid event");
				return;
			}

			AddChild(gameEvent);
			_pendingEvents.Add(gameEvent);
			GD.Print($"[EventsManager] Registered event: {gameEvent.EventId}");
		}

		/// <summary>
		/// Start an event by ID.
		/// </summary>
		public void StartEvent(string eventId)
		{
			var eventToStart = _pendingEvents.Find(e => e.EventId == eventId);
			if (eventToStart == null)
			{
				GD.PrintErr($"[EventsManager] Event not found: {eventId}");
				return;
			}

			if (!eventToStart.CanStart())
			{
				GD.Print($"[EventsManager] Event cannot start: {eventId}");
				return;
			}

			_pendingEvents.Remove(eventToStart);
			_activeEvents[eventId] = eventToStart;
			eventToStart.StartEvent();
		}

		/// <summary>
		/// Get an active event by ID.
		/// </summary>
		public GameEvent GetActiveEvent(string eventId)
		{
			return _activeEvents.GetValueOrDefault(eventId);
		}

		/// <summary>
		/// Check if an event is active.
		/// </summary>
		public bool IsEventActive(string eventId)
		{
			return _activeEvents.ContainsKey(eventId);
		}

		private void OnDayStarted(int dayNumber)
		{
			GD.Print($"[EventsManager] Day {dayNumber} started - checking for events to trigger");

			// Start pending events that can now begin
			var eventsToStart = new List<GameEvent>();
			foreach (var pendingEvent in _pendingEvents)
			{
				if (pendingEvent.CanStart())
				{
					eventsToStart.Add(pendingEvent);
				}
			}

			foreach (var eventToStart in eventsToStart)
			{
				_pendingEvents.Remove(eventToStart);
				_activeEvents[eventToStart.EventId] = eventToStart;
				eventToStart.StartEvent();
			}
		}

		private void OnNightStarted()
		{
			// Fail active events when night starts (player didn't complete in time)
			var eventsToFail = new List<GameEvent>(_activeEvents.Values);
			foreach (var activeEvent in eventsToFail)
			{
				if (activeEvent.State == EventState.Active)
				{
					activeEvent.FailEvent();
				}
			}
		}

		private void OnDayCompleted(int dayNumber)
		{
			// Fail any active events that weren't completed (player ran out of time)
			foreach (var activeEvent in _activeEvents.Values)
			{
				if (activeEvent.State == EventState.Active)
				{
					GD.Print($"[EventsManager] Failing active event at day end: {activeEvent.EventId}");
					activeEvent.FailEvent();
				}
			}

			// Cleanup completed/failed events and register new ones for next day
			CleanupFinishedEvents();
		}

		private void CleanupFinishedEvents()
		{
			var eventsToRemove = new List<string>();

			foreach (var (eventId, gameEvent) in _activeEvents)
			{
				if (gameEvent.State == EventState.Completed || gameEvent.State == EventState.Failed)
				{
					gameEvent.CleanupEventEntities();
					eventsToRemove.Add(eventId);
				}
			}

			foreach (var eventId in eventsToRemove)
			{
				var gameEvent = _activeEvents[eventId];
				_activeEvents.Remove(eventId);
				gameEvent.QueueFree();
				GD.Print($"[EventsManager] Cleaned up event: {eventId}");

				// Register a new event of the same type for next day
				if (eventId == "escort_monk")
				{
					RegisterEscortEvent();
					GD.Print($"[EventsManager] Registered new escort event for next day");
				}
			}
		}

		/// <summary>
		/// Cleanup all event entities (called during day transition).
		/// </summary>
		public void CleanupAllEventEntities()
		{
			foreach (var gameEvent in _activeEvents.Values)
			{
				gameEvent.CleanupEventEntities();
			}
		}
	}
}
