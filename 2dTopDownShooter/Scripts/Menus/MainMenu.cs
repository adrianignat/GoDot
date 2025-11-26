using dTopDownShooter.Scripts;
using Godot;

public partial class MainMenu : Control
{
	private Button _startButton;
	private Button _wifeModeButton;
	private bool _gameStarted = false;

	public override void _Ready()
	{
		// Show the menu by default
		Visible = true;
		Game.Instance.IsPaused = true;

		var vbox = GetNode("MarginContainer").GetNode<VBoxContainer>("VBoxContainer");

		var exitButton = vbox.GetNode<Button>("Exit_Game");
		exitButton.Pressed += () => GetTree().Quit();

		_startButton = vbox.GetNode<Button>("Start_Game");
		_startButton.Pressed += OnStartPressed;

		_wifeModeButton = vbox.GetNode<Button>("Wife_Mode");
		_wifeModeButton.Pressed += OnWifeModePressed;
	}

	private void OnStartPressed()
	{
		if (_gameStarted)
		{
			// Pause menu context: Resume or Restart
			if (Game.Instance.Player.IsDead)
			{
				// Restart the game
				Game.Instance.ShouldRestart = true;
			}
			else
			{
				// Just resume
				Visible = false;
				Game.Instance.IsPaused = false;
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
			UpdateMenuForContext();
		}
		else
		{
			Game.Instance.IsPaused = false;
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
}
