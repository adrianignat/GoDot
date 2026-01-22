using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public enum EventState
	{
		Inactive,
		Active,
		Completed,
		Failed
	}

	/// <summary>
	/// Abstract base class for all game events/quests.
	/// Events have a lifecycle: Inactive -> Active -> Completed/Failed
	/// </summary>
	public abstract partial class GameEvent : Node
	{
		public abstract string EventId { get; }
		public abstract string DisplayName { get; }

		public EventState State { get; protected set; } = EventState.Inactive;
		public string CurrentObjective { get; protected set; } = "";

		// Signals
		[Signal]
		public delegate void EventStartedEventHandler(string eventId);

		[Signal]
		public delegate void EventCompletedEventHandler(string eventId);

		[Signal]
		public delegate void EventFailedEventHandler(string eventId);

		[Signal]
		public delegate void ObjectiveChangedEventHandler(string eventId, string objective);

		/// <summary>
		/// Check if this event can be started. Override for specific conditions.
		/// </summary>
		public virtual bool CanStart()
		{
			return State == EventState.Inactive;
		}

		/// <summary>
		/// Start the event. Sets state to Active and calls OnEventStart.
		/// </summary>
		public void StartEvent()
		{
			if (!CanStart())
			{
				GD.Print($"[{EventId}] Cannot start event - state is {State}");
				return;
			}

			State = EventState.Active;
			GD.Print($"[{EventId}] Event started: {DisplayName}");
			EmitSignal(SignalName.EventStarted, EventId);
			OnEventStart();
		}

		/// <summary>
		/// Called every frame while event is active.
		/// </summary>
		public void UpdateEvent(double delta)
		{
			if (State != EventState.Active)
				return;

			OnEventUpdate(delta);
		}

		/// <summary>
		/// Complete the event successfully.
		/// </summary>
		public void CompleteEvent()
		{
			if (State != EventState.Active)
				return;

			State = EventState.Completed;
			GD.Print($"[{EventId}] Event completed: {DisplayName}");
			EmitSignal(SignalName.EventCompleted, EventId);
			OnEventComplete();
		}

		/// <summary>
		/// Fail the event.
		/// </summary>
		public void FailEvent()
		{
			if (State != EventState.Active)
				return;

			State = EventState.Failed;
			GD.Print($"[{EventId}] Event failed: {DisplayName}");
			EmitSignal(SignalName.EventFailed, EventId);
			OnEventFail();
		}

		/// <summary>
		/// Cleanup event entities (allies, markers, buildings spawned by this event).
		/// </summary>
		public virtual void CleanupEventEntities()
		{
			// Override in subclasses to cleanup specific entities
		}

		/// <summary>
		/// Set a new objective and emit signal.
		/// </summary>
		protected void SetObjective(string objective)
		{
			CurrentObjective = objective;
			GD.Print($"[{EventId}] New objective: {objective}");
			EmitSignal(SignalName.ObjectiveChanged, EventId, objective);
		}

		/// <summary>
		/// Override to handle event start logic.
		/// </summary>
		protected abstract void OnEventStart();

		/// <summary>
		/// Override to handle event update logic (called every frame while active).
		/// </summary>
		protected abstract void OnEventUpdate(double delta);

		/// <summary>
		/// Override to handle event completion logic.
		/// </summary>
		protected abstract void OnEventComplete();

		/// <summary>
		/// Override to handle event failure logic.
		/// </summary>
		protected abstract void OnEventFail();
	}
}
