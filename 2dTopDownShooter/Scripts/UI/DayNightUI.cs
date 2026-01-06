using Godot;

namespace dTopDownShooter.Scripts.UI
{
	public partial class DayNightUI : CanvasLayer
	{
		private Label _dayLabel;
		private Label _timerLabel;
		private Label _phaseLabel;
		private Label _shelterIndicator;
		private DayNightManager _dayNightManager;
		private Control _dayAnnouncement;
		private Label _dayAnnouncementLabel;

		public override void _Ready()
		{
			// Create the UI elements
			CreateTimerUI();
			CreateDayAnnouncement();
			CreateShelterIndicator();

			// Connect to game signals
			Game.Instance.DayStarted += OnDayStarted;
			Game.Instance.ShelterWarning += OnShelterWarning;
			Game.Instance.NightStarted += OnNightStarted;
			Game.Instance.PlayerEnteredShelter += OnPlayerEnteredShelter;
		}

		public void SetDayNightManager(DayNightManager manager)
		{
			_dayNightManager = manager;
		}

		private void CreateTimerUI()
		{
			// Timer container in top right
			var timerContainer = new VBoxContainer();
			timerContainer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
			timerContainer.Position = new Vector2(-200, 10);
			timerContainer.Size = new Vector2(190, 80);
			AddChild(timerContainer);

			// Day label
			_dayLabel = new Label();
			_dayLabel.Text = "Day 1";
			_dayLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_dayLabel.AddThemeColorOverride("font_color", Colors.White);
			_dayLabel.AddThemeFontSizeOverride("font_size", 24);
			timerContainer.AddChild(_dayLabel);

			// Phase label
			_phaseLabel = new Label();
			_phaseLabel.Text = "Daylight left";
			_phaseLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_phaseLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_phaseLabel.AddThemeFontSizeOverride("font_size", 16);
			timerContainer.AddChild(_phaseLabel);

			// Timer label
			_timerLabel = new Label();
			_timerLabel.Text = "3:00";
			_timerLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_timerLabel.AddThemeColorOverride("font_color", Colors.White);
			_timerLabel.AddThemeFontSizeOverride("font_size", 32);
			timerContainer.AddChild(_timerLabel);
		}

		private void CreateDayAnnouncement()
		{
			// Full screen announcement for day start
			_dayAnnouncement = new Control();
			_dayAnnouncement.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_dayAnnouncement.MouseFilter = Control.MouseFilterEnum.Ignore;
			_dayAnnouncement.Visible = false;
			AddChild(_dayAnnouncement);

			_dayAnnouncementLabel = new Label();
			_dayAnnouncementLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
			_dayAnnouncementLabel.GrowHorizontal = Control.GrowDirection.Both;
			_dayAnnouncementLabel.GrowVertical = Control.GrowDirection.Both;
			_dayAnnouncementLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_dayAnnouncementLabel.VerticalAlignment = VerticalAlignment.Center;
			_dayAnnouncementLabel.AddThemeColorOverride("font_color", Colors.White);
			_dayAnnouncementLabel.AddThemeFontSizeOverride("font_size", 72);
			_dayAnnouncement.AddChild(_dayAnnouncementLabel);
		}

		private void CreateShelterIndicator()
		{
			// Shelter status indicator
			_shelterIndicator = new Label();
			_shelterIndicator.SetAnchorsPreset(Control.LayoutPreset.TopWide);
			_shelterIndicator.Position = new Vector2(0, 100);
			_shelterIndicator.HorizontalAlignment = HorizontalAlignment.Center;
			_shelterIndicator.AddThemeColorOverride("font_color", Colors.Green);
			_shelterIndicator.AddThemeFontSizeOverride("font_size", 24);
			_shelterIndicator.Visible = false;
			AddChild(_shelterIndicator);
		}

		public override void _Process(double delta)
		{
			if (_dayNightManager == null || !_dayNightManager.IsStarted)
				return;

			// Update timer
			_timerLabel.Text = _dayNightManager.GetTimeString();
			_phaseLabel.Text = _dayNightManager.GetPhaseLabel();

			// Update colors based on phase
			switch (Game.Instance.CurrentPhase)
			{
				case GamePhase.Day:
					_timerLabel.AddThemeColorOverride("font_color", Colors.White);
					_phaseLabel.AddThemeColorOverride("font_color", Colors.Yellow);
					break;
				case GamePhase.ShelterWarning:
					_timerLabel.AddThemeColorOverride("font_color", Colors.Orange);
					_phaseLabel.AddThemeColorOverride("font_color", Colors.Red);
					break;
				case GamePhase.Night:
					_timerLabel.AddThemeColorOverride("font_color", Colors.Purple);
					_phaseLabel.AddThemeColorOverride("font_color", Colors.DarkViolet);
					break;
			}

			// Update shelter indicator
			if (Game.Instance.CurrentPhase == GamePhase.ShelterWarning ||
				Game.Instance.CurrentPhase == GamePhase.Night)
			{
				_shelterIndicator.Visible = true;
				if (Game.Instance.IsInShelter)
				{
					_shelterIndicator.Text = "IN SHELTER - SAFE";
					_shelterIndicator.AddThemeColorOverride("font_color", Colors.Green);
				}
				else
				{
					_shelterIndicator.Text = "FIND SHELTER!";
					_shelterIndicator.AddThemeColorOverride("font_color", Colors.Red);
				}
			}
			else
			{
				_shelterIndicator.Visible = false;
			}
		}

		private void OnDayStarted(int dayNumber)
		{
			_dayLabel.Text = $"Day {dayNumber}";
			ShowDayAnnouncement(dayNumber);
		}

		private async void ShowDayAnnouncement(int dayNumber)
		{
			_dayAnnouncementLabel.Text = $"Day {dayNumber}";
			_dayAnnouncementLabel.Modulate = new Color(1, 1, 1, 0);
			_dayAnnouncement.Visible = true;

			// Fade in
			var fadeIn = CreateTween();
			fadeIn.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 1), 0.5f);
			await ToSignal(fadeIn, Tween.SignalName.Finished);

			// Hold
			await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

			// Fade out
			var fadeOut = CreateTween();
			fadeOut.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 0), 0.5f);
			await ToSignal(fadeOut, Tween.SignalName.Finished);

			_dayAnnouncement.Visible = false;
		}

		private void OnShelterWarning()
		{
			// Flash warning
			FlashWarning("FIND SHELTER!", Colors.Red);
		}

		private void OnNightStarted()
		{
			FlashWarning("NIGHT HAS FALLEN!", Colors.Purple);
		}

		private void OnPlayerEnteredShelter()
		{
			if (Game.Instance.CurrentPhase == GamePhase.ShelterWarning)
			{
				FlashWarning("SAFE!", Colors.Green);
			}
		}

		private async void FlashWarning(string message, Color color)
		{
			_dayAnnouncementLabel.Text = message;
			_dayAnnouncementLabel.AddThemeColorOverride("font_color", color);
			_dayAnnouncementLabel.Modulate = new Color(1, 1, 1, 0);
			_dayAnnouncement.Visible = true;

			// Fade in
			var fadeIn = CreateTween();
			fadeIn.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 1), 0.3f);
			await ToSignal(fadeIn, Tween.SignalName.Finished);

			// Hold
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

			// Fade out
			var fadeOut = CreateTween();
			fadeOut.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 0), 0.3f);
			await ToSignal(fadeOut, Tween.SignalName.Finished);

			_dayAnnouncement.Visible = false;
			_dayAnnouncementLabel.AddThemeColorOverride("font_color", Colors.White);
		}
	}
}
