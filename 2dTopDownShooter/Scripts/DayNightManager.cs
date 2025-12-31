using dTopDownShooter.Scripts.UI;
using Godot;

namespace dTopDownShooter.Scripts
{
	public partial class DayNightManager : Node
	{
		private float _timeRemaining;
		private float _nightDamageTimer;
		private Vector2 _currentShelterPosition;
		private bool _shelterMarked = false;
		private bool _isTransitioning = false;
		private bool _isStarted = false;
		private bool _survivedNight = false;
		private MapGenerator _mapGenerator;
		private ColorRect _fadeOverlay;
		private NightSurvivedUI _nightSurvivedUI;

		public float TimeRemaining => _timeRemaining;
		public Vector2 CurrentShelterPosition => _currentShelterPosition;
		public bool IsShelterMarked => _shelterMarked;
		public bool IsTransitioning => _isTransitioning;
		public bool IsStarted => _isStarted;

		public override void _Ready()
		{
			_mapGenerator = GetTree().Root.GetNode("main").GetNodeOrNull<MapGenerator>("MapGenerator");
			CreateFadeOverlay();
			CreateNightSurvivedUI();
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

		private void CreateNightSurvivedUI()
		{
			_nightSurvivedUI = new NightSurvivedUI();
			_nightSurvivedUI.Name = "NightSurvivedUI";
			AddChild(_nightSurvivedUI);

			_nightSurvivedUI.ContinueSelected += OnContinueSelected;
			_nightSurvivedUI.NewGameSelected += OnNewGameSelected;
		}

		private void OnContinueSelected()
		{
			int completedDay = Game.Instance.CurrentDay;
			StartDayTransition(completedDay + 1);
		}

		private void OnNewGameSelected()
		{
			Game.Instance.ShouldRestart = true;
		}

		/// <summary>
		/// Starts the first day of the game. Call this once when gameplay begins.
		/// </summary>
		public void StartDay(int dayNumber)
		{
			_isStarted = true;
			InitializeDayState(dayNumber);
		}

		private void InitializeDayState(int dayNumber)
		{
			Game.Instance.CurrentDay = dayNumber;
			Game.Instance.CurrentPhase = GamePhase.Day;
			Game.Instance.IsInShelter = false;
			_timeRemaining = GameConstants.DayDuration;
			_shelterMarked = false;
			_survivedNight = false;
			_nightDamageTimer = GameConstants.NightDamageInterval;

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
			if (_timeRemaining <= GameConstants.ShelterWarningTime && !_shelterMarked)
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

			// Pick shelter - prioritizes built house ruins if available
			if (_mapGenerator != null)
			{
				_currentShelterPosition = _mapGenerator.GetShelterPosition();
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
			Game.Instance.IsInShelter = distance <= GameConstants.ShelterRadius;

			if (Game.Instance.IsInShelter && !wasInShelter)
			{
				Game.Instance.EmitSignal(Game.SignalName.PlayerEnteredShelter);
				GD.Print("Player entered shelter!");
			}
		}

		private void StartNight()
		{
			Game.Instance.CurrentPhase = GamePhase.Night;
			_timeRemaining = GameConstants.NightDuration;
			_nightDamageTimer = GameConstants.NightDamageInterval;

			Game.Instance.EmitSignal(Game.SignalName.NightStarted);
			GD.Print("Night has fallen! You'll take damage until dawn.");
		}

		private void ProcessNight(float delta)
		{
			_timeRemaining -= delta;
			_nightDamageTimer -= delta;

			// Check if player found shelter during night
			CheckShelterProximity();

			// If player reaches shelter during night, complete the day normally
			if (Game.Instance.IsInShelter)
			{
				GD.Print("Player reached shelter during night - safe!");
				CompleteDay();
				return;
			}

			// Deal damage every few seconds if not in shelter
			if (_nightDamageTimer <= 0)
			{
				Game.Instance.EmitSignal(Game.SignalName.NightDamageTick);
				GD.Print("Night damage!");
				_nightDamageTimer = GameConstants.NightDamageInterval;
			}

			// Night ended - player survived!
			if (_timeRemaining <= 0)
			{
				GD.Print("Player survived the night - Night Walker!");
				ShowNightSurvivedScreen();
			}
		}

		private void ShowNightSurvivedScreen()
		{
			if (_survivedNight) return; // Prevent showing twice

			_survivedNight = true;
			int completedDay = Game.Instance.CurrentDay;
			Game.Instance.EmitSignal(Game.SignalName.DayCompleted, completedDay);
			_nightSurvivedUI.ShowNightSurvived(completedDay);
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

			// Pause the game during transition
			Game.Instance.IsPaused = true;

			// Fade to black
			var fadeOutTween = CreateTween();
			fadeOutTween.TweenProperty(_fadeOverlay, "color", new Color(0, 0, 0, 1), GameConstants.FadeDuration);
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

			// Start new day (reuses same initialization as StartDay)
			InitializeDayState(nextDay);

			// Fade back in
			var fadeInTween = CreateTween();
			fadeInTween.TweenProperty(_fadeOverlay, "color", new Color(0, 0, 0, 0), GameConstants.FadeDuration);
			await ToSignal(fadeInTween, Tween.SignalName.Finished);

			_isTransitioning = false;

			// Unpause the game after transition
			Game.Instance.IsPaused = false;
		}

		private void CleanupEntities()
		{
			// Remove all enemies
			var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);
			foreach (Node enemy in enemies)
			{
				enemy.QueueFree();
			}

			// Remove all gold
			var goldNodes = GetTree().GetNodesInGroup(GameConstants.GoldGroup);
			foreach (Node gold in goldNodes)
			{
				gold.QueueFree();
			}

			// Remove all dynamite (player and enemy)
			var dynamiteNodes = GetTree().GetNodesInGroup(GameConstants.DynamiteGroup);
			foreach (Node dynamite in dynamiteNodes)
			{
				dynamite.QueueFree();
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
