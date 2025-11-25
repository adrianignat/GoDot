using dTopDownShooter.Scripts;
using Godot;

public partial class MainMenu : Control
{
	Button startButton;
	public override void _Ready()
	{
		// Show the pause menu by default
		Visible = true;
		Game.Instance.IsPaused = true;

		var vbox = GetNode("MarginContainer").GetNode<VBoxContainer>("VBoxContainer");
		var exitButton = vbox.GetNode<Button>("Exit_Game");
		exitButton.Pressed += () =>
		{
			GetTree().Quit();
		};

		startButton = vbox.GetNode<Button>("Start_Game");
		startButton.Pressed += OnStartPressed;
	}

	private bool _gameStarted = false;

	private void OnStartPressed()
	{
		Visible = false;
		Game.Instance.IsPaused = false;

		if (Game.Instance.Player.IsDead)
		{
			Game.Instance.ShouldRestart = true;
			_gameStarted = false;
		}

		// Start the first day when game begins
		if (!_gameStarted)
		{
			Game.Instance.StartFirstDay();
			_gameStarted = true;
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Detect Esc key press
		if (@event.IsActionPressed("pause"))
		{
			ToggleMenu();
		}
	}

	private void ToggleMenu()
	{
		// Toggle the visibility of the pause menu
		Visible = !Visible;
		if (Game.Instance.Player.IsDead)
		{
			startButton.Text = "Restart";
		}
		else
		{
			startButton.Text = "Resume";
		}
		
		if (Visible)
		{
			Game.Instance.IsPaused = true;
		}
		else
		{
			Game.Instance.IsPaused = false;
		}
	}
}
