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
		private ulong _announcementVersion;

		public override void _Ready()
		{
			CreateTimerUI();
			CreateDayAnnouncement();
			CreateShelterIndicator();

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
			var timerContainer = new VBoxContainer();
			timerContainer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
			timerContainer.Position = new Vector2(-200, 10);
			timerContainer.Size = new Vector2(190, 80);
			AddChild(timerContainer);

			_dayLabel = new Label();
			_dayLabel.Text = "Day 1";
			_dayLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_dayLabel.AddThemeColorOverride("font_color", Colors.White);
			_dayLabel.AddThemeFontSizeOverride("font_size", 24);
			timerContainer.AddChild(_dayLabel);

			_phaseLabel = new Label();
			_phaseLabel.Text = "Daylight left";
			_phaseLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_phaseLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_phaseLabel.AddThemeFontSizeOverride("font_size", 16);
			timerContainer.AddChild(_phaseLabel);

			_timerLabel = new Label();
			_timerLabel.Text = "3:00";
			_timerLabel.HorizontalAlignment = HorizontalAlignment.Right;
			_timerLabel.AddThemeColorOverride("font_color", Colors.White);
			_timerLabel.AddThemeFontSizeOverride("font_size", 32);
			timerContainer.AddChild(_timerLabel);
		}

		private void CreateDayAnnouncement()
		{
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
			_dayAnnouncementLabel.AddThemeConstantOverride("outline_size", 4);
			_dayAnnouncementLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_dayAnnouncement.AddChild(_dayAnnouncementLabel);
		}

		private void CreateShelterIndicator()
		{
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

			_timerLabel.Text = _dayNightManager.GetTimeString();
			_phaseLabel.Text = _dayNightManager.GetPhaseLabel();

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
			ShowAnnouncement($"Day {dayNumber}", Colors.White, 1.5f, 0.5f, 72);
		}

		private void OnShelterWarning()
		{
			ShowAnnouncement("FIND SHELTER!", Colors.Red, 1.0f, 0.3f, 72);
		}

		private void OnNightStarted()
		{
			ShowAnnouncement("NIGHT HAS FALLEN!", Colors.Purple, 1.0f, 0.3f, 72);
		}

		private void OnPlayerEnteredShelter()
		{
			if (Game.Instance.CurrentPhase == GamePhase.ShelterWarning)
				ShowAnnouncement("SAFE!", Colors.Green, 1.0f, 0.3f, 72);
		}

		public async void ShowAnnouncement(string message, Color color, float holdDuration = 1.0f, float fadeDuration = 0.3f, int fontSize = 72, float delay = 0f)
		{
			ulong version = ++_announcementVersion;

			if (delay > 0f)
			{
				await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
				if (version != _announcementVersion)
					return;
			}

			_dayAnnouncementLabel.Text = message;
			_dayAnnouncementLabel.AddThemeColorOverride("font_color", color);
			_dayAnnouncementLabel.AddThemeFontSizeOverride("font_size", fontSize);
			_dayAnnouncementLabel.Modulate = new Color(1, 1, 1, 0);
			_dayAnnouncement.Visible = true;

			var fadeIn = CreateTween();
			fadeIn.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 1), fadeDuration);
			await ToSignal(fadeIn, Tween.SignalName.Finished);
			if (version != _announcementVersion)
				return;

			await ToSignal(GetTree().CreateTimer(holdDuration), SceneTreeTimer.SignalName.Timeout);
			if (version != _announcementVersion)
				return;

			var fadeOut = CreateTween();
			fadeOut.TweenProperty(_dayAnnouncementLabel, "modulate", new Color(1, 1, 1, 0), fadeDuration);
			await ToSignal(fadeOut, Tween.SignalName.Finished);
			if (version != _announcementVersion)
				return;

			_dayAnnouncement.Visible = false;
			_dayAnnouncementLabel.AddThemeColorOverride("font_color", Colors.White);
			_dayAnnouncementLabel.AddThemeFontSizeOverride("font_size", 72);
		}
	}
}
