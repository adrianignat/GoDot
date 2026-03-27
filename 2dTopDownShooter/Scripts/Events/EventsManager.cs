using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dTopDownShooter.Scripts.Events
{
	public partial class EventsManager : Node
	{
		private const int EventsPerDay = 2;

		private readonly Dictionary<string, Func<GameEvent>> _eventFactories = new();
		private readonly Dictionary<string, GameEvent> _activeEvents = new();
		private readonly Queue<string> _queuedEventIds = new();
		private readonly Random _random = new();
		private string _lastEventId = string.Empty;

		public override void _Ready()
		{
			Game.Instance.DayStarted += OnDayStarted;
			Game.Instance.NightStarted += OnNightStarted;
			Game.Instance.DayCompleted += OnDayCompleted;

			RegisterDefaultEvents();
		}

		private void RegisterDefaultEvents()
		{
			RegisterEventFactory("escort_monk", () => new EscortEvent());
			RegisterEventFactory("help_knight", () => new KnightGoblinEvent());
			RegisterEventFactory("build_archer_tower", () => new BuildArcherTowerEvent());
			RegisterEventFactory("deplete_gold_mine", () => new DepleteGoldMineEvent());
			RegisterEventFactory("gather_pink_sheep", () => new GatherPinkSheepEvent());
			RegisterEventFactory("gather_lumber", () => new GatherLumberEvent());
			RegisterEventFactory("kill_enemy_type", () => new KillEnemyTypeEvent());
		}

		public override void _Process(double delta)
		{
			foreach (var gameEvent in _activeEvents.Values.ToList())
				gameEvent.UpdateEvent(delta);

			CleanupFinishedEvents();
		}

		public void RegisterEventFactory(string eventId, Func<GameEvent> factory)
		{
			_eventFactories[eventId] = factory;
		}

		public void StartEvent(string eventId)
		{
			if (!_eventFactories.ContainsKey(eventId))
			{
				GD.PrintErr($"[EventsManager] Event factory not found: {eventId}");
				return;
			}

			if (_activeEvents.ContainsKey(eventId) || _queuedEventIds.Contains(eventId))
			{
				GD.Print($"[EventsManager] Skipping duplicate event start: {eventId}");
				return;
			}

			if (_activeEvents.Count == 0)
			{
				StartEventInstance(_eventFactories[eventId]());
				return;
			}

			_queuedEventIds.Enqueue(eventId);
		}

		private void StartEventInstance(GameEvent gameEvent)
		{
			if (!IsInstanceValid(gameEvent))
			{
				GD.PrintErr("[EventsManager] Attempted to start invalid event instance");
				return;
			}

			if (_activeEvents.Count > 0 || _activeEvents.ContainsKey(gameEvent.EventId))
			{
				GD.Print($"[EventsManager] Skipping overlapping event start: {gameEvent.EventId}");
				gameEvent.QueueFree();
				return;
			}

			AddChild(gameEvent);
			_activeEvents[gameEvent.EventId] = gameEvent;
			_lastEventId = gameEvent.EventId;
			gameEvent.StartEvent();
		}

		public GameEvent GetActiveEvent(string eventId)
		{
			return _activeEvents.GetValueOrDefault(eventId);
		}

		public bool IsEventActive(string eventId)
		{
			return _activeEvents.ContainsKey(eventId);
		}

		private async void OnDayStarted(int dayNumber)
		{
			if (_eventFactories.Count == 0)
				return;

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			CleanupFinishedEvents();
			_queuedEventIds.Clear();

			var availableFactories = _eventFactories.Keys.ToList();
			if (availableFactories.Count == 0)
				return;

			int eventsToQueue = Math.Min(EventsPerDay, availableFactories.Count);
			if (availableFactories.Count > eventsToQueue && !string.IsNullOrEmpty(_lastEventId))
				availableFactories.Remove(_lastEventId);

			availableFactories = availableFactories
				.OrderBy(_ => _random.Next())
				.ToList();

			foreach (string selectedId in availableFactories.Take(eventsToQueue))
				_queuedEventIds.Enqueue(selectedId);

			TryStartNextQueuedEvent();
		}

		private void OnNightStarted()
		{
			_queuedEventIds.Clear();
			foreach (var activeEvent in _activeEvents.Values.ToList())
			{
				if (activeEvent.State == EventState.Active)
					activeEvent.FailEvent();
			}
		}

		private void OnDayCompleted(int dayNumber)
		{
			_queuedEventIds.Clear();
			foreach (var activeEvent in _activeEvents.Values.ToList())
			{
				if (activeEvent.State == EventState.Active)
					activeEvent.FailEvent();
			}

			CleanupFinishedEvents();
		}

		private void CleanupFinishedEvents()
		{
			bool removedFinishedEvent = false;
			foreach (var (eventId, gameEvent) in _activeEvents.ToList())
			{
				if (gameEvent.State != EventState.Completed && gameEvent.State != EventState.Failed)
					continue;

				gameEvent.CleanupEventEntities();
				_activeEvents.Remove(eventId);
				gameEvent.QueueFree();
				removedFinishedEvent = true;
			}

			if (removedFinishedEvent)
				TryStartNextQueuedEvent();
		}

		private void TryStartNextQueuedEvent()
		{
			if (_activeEvents.Count > 0 || _queuedEventIds.Count == 0)
				return;

			if (Game.Instance.CurrentPhase != GamePhase.Day)
				return;

			while (_queuedEventIds.Count > 0)
			{
				string nextEventId = _queuedEventIds.Dequeue();
				if (!_eventFactories.TryGetValue(nextEventId, out var factory))
					continue;

				StartEventInstance(factory());
				break;
			}
		}

		public void CleanupAllEventEntities()
		{
			foreach (var gameEvent in _activeEvents.Values)
				gameEvent.CleanupEventEntities();
		}
	}
}
