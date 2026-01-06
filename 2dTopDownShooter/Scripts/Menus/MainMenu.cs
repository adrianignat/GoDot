using dTopDownShooter.Scripts;
using Godot;

public partial class MainMenu : Control
{
	private Button _startButton;
	private Button _wifeModeButton;
	private Button _optionsButton;
	private Button _shootingStyleButton;
	private VBoxContainer _mainMenuContainer;
	private VBoxContainer _optionsContainer;
	private bool _gameStarted = false;

	public override void _Ready()
	{
		// UI must process during pause to handle input
		ProcessMode = ProcessModeEnum.Always;

		// Show the menu by default
		Visible = true;
		Game.Instance.IsPaused = true;

		var marginContainer = GetNode("MarginContainer");
		_mainMenuContainer = marginContainer.GetNode<VBoxContainer>("VBoxContainer");
		_optionsContainer = marginContainer.GetNode<VBoxContainer>("OptionsContainer");

		var exitButton = _mainMenuContainer.GetNode<Button>("Exit_Game");
		exitButton.Pressed += () => GetTree().Quit();

		_startButton = _mainMenuContainer.GetNode<Button>("Start_Game");
		_startButton.Pressed += OnStartPressed;

		_wifeModeButton = _mainMenuContainer.GetNode<Button>("Wife_Mode");
		_wifeModeButton.Pressed += OnWifeModePressed;

		_optionsButton = _mainMenuContainer.GetNode<Button>("Game_Options");
		_optionsButton.Pressed += OnOptionsPressed;

		// Options panel buttons
		_shootingStyleButton = _optionsContainer.GetNode<Button>("ShootingStyleButton");
		_shootingStyleButton.Pressed += OnShootingStylePressed;
		UpdateShootingStyleButton();

		var backButton = _optionsContainer.GetNode<Button>("BackButton");
		backButton.Pressed += OnBackPressed;
	}

	private void OnStartPressed()
	{
		if (_gameStarted)
		{
			// Pause menu context: Resume or Restart
			if (Game.Instance.Player.IsDead)
			{
				// Restart the game directly (MainMenu has ProcessMode.Always)
				Game.Instance.IsPaused = false;
				Game.Instance.Restart();
			}
			else
			{
				// Just resume - but don't unpause if upgrade selection is showing
				Visible = false;
				if (!Game.Instance.IsUpgradeSelectionShowing)
				{
					Game.Instance.IsPaused = false;
				}
			}
		}
		else
		{
			// Main menu context: Start new game (normal mode)
			Game.Instance.WifeMode = false;
			StartNewGame();
		}
	}

	private void OnWifeModePressed()
	{
		// Only available from main menu (before game started)
		Game.Instance.WifeMode = true;
		StartNewGame();
	}

	private void StartNewGame()
	{
		Visible = false;
		Game.Instance.IsPaused = false;
		Game.Instance.ApplyGameMode();
		Game.Instance.StartFirstDay();
		_gameStarted = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("pause"))
		{
			ToggleMenu();
		}
	}

	private void ToggleMenu()
	{
		Visible = !Visible;

		if (Visible)
		{
			Game.Instance.IsPaused = true;
			// Always show main menu when opening, hide options
			_mainMenuContainer.Visible = true;
			_optionsContainer.Visible = false;
			UpdateMenuForContext();
		}
		else
		{
			// Don't unpause if upgrade selection is showing or player is dead
			if (!Game.Instance.IsUpgradeSelectionShowing && !Game.Instance.Player.IsDead)
			{
				Game.Instance.IsPaused = false;
			}
		}
	}

	private void UpdateMenuForContext()
	{
		if (_gameStarted)
		{
			// Pause menu: hide Wife Mode, show Resume/Restart
			_wifeModeButton.Visible = false;
			_startButton.Text = Game.Instance.Player.IsDead ? "Restart" : "Resume";
		}
		else
		{
			// Main menu: show all options
			_wifeModeButton.Visible = true;
			_startButton.Text = "Start";
		}
	}

	private void OnOptionsPressed()
	{
		_mainMenuContainer.Visible = false;
		_optionsContainer.Visible = true;
	}

	private void OnBackPressed()
	{
		_optionsContainer.Visible = false;
		_mainMenuContainer.Visible = true;
	}

	private void OnShootingStylePressed()
	{
		// Toggle between Auto-Aim and Directional
		GameSettings.ShootingStyle = GameSettings.ShootingStyle == ShootingStyle.AutoAim
			? ShootingStyle.Directional
			: ShootingStyle.AutoAim;

		UpdateShootingStyleButton();
	}

	private void UpdateShootingStyleButton()
	{
		string styleName = GameSettings.ShootingStyle == ShootingStyle.AutoAim
			? "Auto-Aim"
			: "Directional";
		_shootingStyleButton.Text = $"Aim: {styleName}";
	}
}
