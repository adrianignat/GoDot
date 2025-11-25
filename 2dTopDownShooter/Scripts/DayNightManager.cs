using Godot;

namespace dTopDownShooter.Scripts
{
	public partial class DayNightManager : Node
	{
		// Day duration: 3 minutes = 180 seconds
		private const float DayDuration = 180f;
		// Shelter warning starts at 30 seconds left
		private const float ShelterWarningTime = 30f;
		// Night duration: 1 minute = 60 seconds
		private const float NightDuration = 60f;
		// Night damage tick interval: 3 seconds
		private const float NightDamageInterval = 3f;
		// Shelter detection radius
		private const float ShelterRadius = 80f;
		// Fade transition duration
		private const float FadeDuration = 1.0f;

		private float _timeRemaining;
		private float _nightDamageTimer;
		private Vector2 _currentShelterPosition;
		private bool _shelterMarked = false;
		private bool _isTransitioning = false;
		private bool _isStarted = false;
		private MapGenerator _mapGenerator;
		private ColorRect _fadeOverlay;

		public float TimeRemaining => _timeRemaining;
		public Vector2 CurrentShelterPosition => _currentShelterPosition;
		public bool IsShelterMarked => _shelterMarked;
		public bool IsTransitioning => _isTransitioning;
		public bool IsStarted => _isStarted;

		public override void _Ready()
		{
			_mapGenerator = GetTree().Root.GetNode("main").GetNodeOrNull<MapGenerator>("MapGenerator");
			CreateFadeOverlay();
		}

		private void CreateFadeOverlay()
		{
			// Create a CanvasLayer to ensure the fade is on top of everything
			var canvasLayer = new CanvasLayer();
			canvasLayer.Layer = 100; // High layer to be on top
			AddChild(canvasLayer);

			_fadeOverlay = new ColorRect();
			_fadeOverlay.Color = new Color(0, 0, 0, 0); // Start transparent
			_fadeOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			canvasLayer.AddChild(_fadeOverlay);
		}

		public void StartDay(int dayNumber)
		{
			_isStarted = true;
			Game.Instance.CurrentDay = dayNumber;
			Game.Instance.CurrentPhase = GamePhase.Day;
			Game.Instance.IsInShelter = false;
			_timeRemaining = DayDuration;
			_shelterMarked = false;
			_nightDamageTimer = NightDamageInterval;

			Game.Instance.EmitSignal(Game.SignalName.DayStarted, dayNumber);
			GD.Print($"Day {dayNumber} started!");
		}

		public override void _Process(double delta)
		{
			if (!_isStarted || Game.Instance.IsPaused || _isTransitioning)
				return;

			switch (Game.Instance.CurrentPhase)
			{
				case GamePhase.Day:
					ProcessDay((float)delta);
					break;
				case GamePhase.ShelterWarning:
					ProcessShelterWarning((float)delta);
					break;
				case GamePhase.Night:
					ProcessNight((float)delta);
					break;
			}
		}

		private void ProcessDay(float delta)
		{
			_timeRemaining -= delta;

			// Check if it's time to show shelter warning
			if (_timeRemaining <= ShelterWarningTime && !_shelterMarked)
			{
				EnterShelterWarningPhase();
			}

			// Day ended without reaching shelter
			if (_timeRemaining <= 0)
			{
				StartNight();
			}
		}

		private void EnterShelterWarningPhase()
		{
			Game.Instance.CurrentPhase = GamePhase.ShelterWarning;
			_shelterMarked = true;

			// Pick a random house as shelter
			if (_mapGenerator != null)
			{
				_currentShelterPosition = _mapGenerator.GetRandomHousePosition();
			}
			else
			{
				_currentShelterPosition = new Vector2(640, 640); // Fallback to center
			}

			Game.Instance.EmitSignal(Game.SignalName.ShelterWarning);
			GD.Print($"Shelter warning! Find shelter at {_currentShelterPosition}");
		}

		private void ProcessShelterWarning(float delta)
		{
			_timeRemaining -= delta;

			// Check if player reached shelter
			CheckShelterProximity();

			if (Game.Instance.IsInShelter)
			{
				// Player made it to shelter - complete day
				CompleteDay();
				return;
			}

			// Day ended without reaching shelter
			if (_timeRemaining <= 0)
			{
				StartNight();
			}
		}

		private void CheckShelterProximity()
		{
			var player = Game.Instance.Player;
			if (player == null) return;

			float distance = player.GlobalPosition.DistanceTo(_currentShelterPosition);
			bool wasInShelter = Game.Instance.IsInShelter;
			Game.Instance.IsInShelter = distance <= ShelterRadius;

			if (Game.Instance.IsInShelter && !wasInShelter)
			{
				Game.Instance.EmitSignal(Game.SignalName.PlayerEnteredShelter);
				GD.Print("Player entered shelter!");
			}
		}

		private void StartNight()
		{
			Game.Instance.CurrentPhase = GamePhase.Night;
			_timeRemaining = NightDuration;
			_nightDamageTimer = NightDamageInterval;

			Game.Instance.EmitSignal(Game.SignalName.NightStarted);
			GD.Print("Night has fallen! You'll take damage until dawn.");
		}

		private void ProcessNight(float delta)
		{
			_timeRemaining -= delta;
			_nightDamageTimer -= delta;

			// Check if player found shelter during night
			CheckShelterProximity();

			// If player reaches shelter during night, complete the day early
			if (Game.Instance.IsInShelter)
			{
				GD.Print("Player reached shelter during night - escaping to next day!");
				CompleteDay();
				return;
			}

			// Deal damage every 3 seconds if not in shelter
			if (_nightDamageTimer <= 0)
			{
				Game.Instance.EmitSignal(Game.SignalName.NightDamageTick);
				GD.Print("Night damage!");
				_nightDamageTimer = NightDamageInterval;
			}

			// Night ended
			if (_timeRemaining <= 0)
			{
				CompleteDay();
			}
		}

		private void CompleteDay()
		{
			if (_isTransitioning) return;

			int completedDay = Game.Instance.CurrentDay;
			Game.Instance.EmitSignal(Game.SignalName.DayCompleted, completedDay);
			GD.Print($"Day {completedDay} completed!");

			// Start fade transition to next day
			StartDayTransition(completedDay + 1);
		}

		private async void StartDayTransition(int nextDay)
		{
			_isTransitioning = true;

			// Fade to black
			var fadeOutTween = CreateTween();
			fadeOutTween.TweenProperty(_fadeOverlay, "color", new Color(0, 0, 0, 1), FadeDuration);
			await ToSignal(fadeOutTween, Tween.SignalName.Finished);

			// Cleanup: remove all enemies and gold
			CleanupEntities();

			// Regenerate map
			if (_mapGenerator != null)
			{
				RegenerateMap();
			}

			// Reset player position
			var player = Game.Instance.Player;
			if (player != null && _mapGenerator != null)
			{
				player.Position = _mapGenerator.GetPlayerSpawnPosition();
			}

			// Start new day
			Game.Instance.CurrentDay = nextDay;
			Game.Instance.CurrentPhase = GamePhase.Day;
			Game.Instance.IsInShelter = false;
			_timeRemaining = DayDuration;
			_shelterMarked = false;
			_nightDamageTimer = NightDamageInterval;

			Game.Instance.EmitSignal(Game.SignalName.DayStarted, nextDay);
			GD.Print($"Day {nextDay} started!");

			// Fade back in
			var fadeInTween = CreateTween();
			fadeInTween.TweenProperty(_fadeOverlay, "color", new Color(0, 0, 0, 0), FadeDuration);
			await ToSignal(fadeInTween, Tween.SignalName.Finished);

			_isTransitioning = false;
		}

		private void CleanupEntities()
		{
			// Remove all enemies
			var enemies = GetTree().GetNodesInGroup("enemies");
			foreach (Node enemy in enemies)
			{
				enemy.QueueFree();
			}

			// Remove all gold
			var goldNodes = GetTree().GetNodesInGroup("gold");
			foreach (Node gold in goldNodes)
			{
				gold.QueueFree();
			}

			GD.Print("Cleaned up entities for day transition");
		}

		private void RegenerateMap()
		{
			_mapGenerator.Regenerate();
		}

		public string GetTimeString()
		{
			int minutes = (int)(_timeRemaining / 60);
			int seconds = (int)(_timeRemaining % 60);
			return $"{minutes}:{seconds:D2}";
		}

		public string GetPhaseLabel()
		{
			return Game.Instance.CurrentPhase switch
			{
				GamePhase.Day => "Daylight left",
				GamePhase.ShelterWarning => "FIND SHELTER!",
				GamePhase.Night => "Survive the night",
				_ => ""
			};
		}
	}
}
