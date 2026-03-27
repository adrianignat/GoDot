using dTopDownShooter.Scripts.UI;
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

	public abstract partial class GameEvent : Node
	{
		public abstract string EventId { get; }
		public abstract string DisplayName { get; }
		public virtual ushort CompletionRewardGold => 10;
		public virtual float TimeLimitSeconds => 90f;
		protected virtual EventMarker QuestMarker => null;

		public EventState State { get; protected set; } = EventState.Inactive;
		public string CurrentObjective { get; protected set; } = "";
		public float TimeRemainingSeconds { get; protected set; }

		[Signal]
		public delegate void EventStartedEventHandler(string eventId);

		[Signal]
		public delegate void EventCompletedEventHandler(string eventId);

		[Signal]
		public delegate void EventFailedEventHandler(string eventId);

		[Signal]
		public delegate void ObjectiveChangedEventHandler(string eventId, string objective);

		public virtual bool CanStart()
		{
			return State == EventState.Inactive;
		}

		public void StartEvent()
		{
			if (!CanStart())
			{
				GD.Print($"[{EventId}] Cannot start event - state is {State}");
				return;
			}

			State = EventState.Active;
			TimeRemainingSeconds = TimeLimitSeconds;
			GD.Print($"[{EventId}] Event started: {DisplayName}");
			OnEventStart();

			if (State != EventState.Active)
				return;

			UpdateQuestTimerUI();
			EmitSignal(SignalName.EventStarted, EventId);
			ShowStartAnnouncement();
		}

		public void UpdateEvent(double delta)
		{
			if (State != EventState.Active)
				return;

			TimeRemainingSeconds = Mathf.Max(0f, TimeRemainingSeconds - (float)delta);
			UpdateQuestTimerUI();
			if (TimeRemainingSeconds <= 0f)
			{
				FailEvent();
				return;
			}

			OnEventUpdate(delta);
		}

		public void CompleteEvent()
		{
			if (State != EventState.Active)
				return;

			State = EventState.Completed;
			AwardCompletionReward();
			GD.Print($"[{EventId}] Event completed: {DisplayName}");
			EmitSignal(SignalName.EventCompleted, EventId);
			OnEventComplete();
			ShowCompletionAnnouncement();
		}

		public void FailEvent()
		{
			if (State != EventState.Active)
				return;

			State = EventState.Failed;
			GD.Print($"[{EventId}] Event failed: {DisplayName}");
			EmitSignal(SignalName.EventFailed, EventId);
			OnEventFail();
		}

		public virtual void CleanupEventEntities()
		{
		}

		protected void SetObjective(string objective)
		{
			CurrentObjective = objective;
			GD.Print($"[{EventId}] New objective: {objective}");
			EmitSignal(SignalName.ObjectiveChanged, EventId, objective);
		}

		protected string GetRemainingTimeText()
		{
			return $"{Mathf.CeilToInt(TimeRemainingSeconds)}s left";
		}

		private void UpdateQuestTimerUI()
		{
			if (QuestMarker != null && GodotObject.IsInstanceValid(QuestMarker))
				QuestMarker.SetTimer(GetRemainingTimeText());
		}

		private void AwardCompletionReward()
		{
			if (CompletionRewardGold <= 0)
				return;

			Game.Instance.EmitSignal(Game.SignalName.GoldAcquired, CompletionRewardGold);
			GD.Print($"[{EventId}] Reward granted: {CompletionRewardGold} gold");
		}

		private void ShowStartAnnouncement()
		{
			var dayNightUi = Game.Instance.GetNodeOrNull<DayNightUI>("DayNightUI");
			if (dayNightUi == null)
				return;

			string message = string.IsNullOrWhiteSpace(CurrentObjective)
				? $"Quest Started\n{DisplayName}\n{Mathf.CeilToInt(TimeLimitSeconds)}s to complete"
				: $"Quest Started\n{CurrentObjective}\n{Mathf.CeilToInt(TimeLimitSeconds)}s to complete";

			dayNightUi.ShowAnnouncement(message, Colors.Gold, 4.8f, 0.35f, 42, 2.1f);
		}

		private void ShowCompletionAnnouncement()
		{
			var dayNightUi = Game.Instance.GetNodeOrNull<DayNightUI>("DayNightUI");
			if (dayNightUi == null)
				return;

			string rewardLine = CompletionRewardGold > 0 ? $"\n+{CompletionRewardGold} Gold" : string.Empty;
			dayNightUi.ShowAnnouncement($"Quest Complete\n{DisplayName}{rewardLine}", Colors.GreenYellow, 1.8f, 0.35f, 40);
		}

		protected abstract void OnEventStart();
		protected abstract void OnEventUpdate(double delta);
		protected abstract void OnEventComplete();
		protected abstract void OnEventFail();
	}
}
