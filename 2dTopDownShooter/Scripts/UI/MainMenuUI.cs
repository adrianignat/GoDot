using dTopDownShooter.Scripts;
using Godot;
using System;

public partial class MainMenuUI : Control
{
	private const int PressedTextOffsetY = 10;

	private TextureButton _startButton;
	private TextureButton _easyButton;
	private TextureButton _optionsButton;
	private TextureButton _exitButton;
	private TextureButton _shootingStyleButton;
	private TextureButton _backButton;

	private VBoxContainer _mainMenuContainer;
	private VBoxContainer _optionsContainer;

	private Label _startLabel;
	private Label _shootingStyleLabel;
	private bool _gameStarted = false;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		Visible = true;
		Game.Instance.IsPaused = true;

		_mainMenuContainer = GetNode<VBoxContainer>("CenterContainer/VBoxContainer");
		_optionsContainer = GetNode<VBoxContainer>("CenterContainer/OptionsContainer");

		_startButton = GetNode<TextureButton>("CenterContainer/VBoxContainer/StartButton");
		_easyButton = GetNode<TextureButton>("CenterContainer/VBoxContainer/EasyButton");
		_optionsButton = GetNode<TextureButton>("CenterContainer/VBoxContainer/OptionsButton");
		_exitButton = GetNode<TextureButton>("CenterContainer/VBoxContainer/ExitButton");
		_shootingStyleButton = GetNode<TextureButton>("CenterContainer/OptionsContainer/ShootingStyleButton");
		_backButton = GetNode<TextureButton>("CenterContainer/OptionsContainer/BackButton");

		_startLabel = _startButton.GetNode<Label>("Label");
		_shootingStyleLabel = _shootingStyleButton.GetNode<Label>("Label");

		SetupButton(_startButton, OnStartPressed);
		SetupButton(_easyButton, OnEasyPressed);
		SetupButton(_optionsButton, OnOptionsPressed);
		SetupButton(_exitButton, OnExitPressed);
		SetupButton(_shootingStyleButton, OnShootingStylePressed);
		SetupButton(_backButton, OnBackPressed);

		UpdateShootingStyleLabel();
	}

	private void SetupButton(TextureButton button, Action onPressed)
	{
		var label = button.GetNode<Label>("Label");

		button.Pressed += () =>
		{
			label.AddThemeConstantOverride("margin_top", PressedTextOffsetY);
			onPressed?.Invoke();
		};

		button.ButtonUp += () =>
		{
			label.AddThemeConstantOverride("margin_top", 0);
		};
	}

	private void OnStartPressed()
	{
		if (_gameStarted)
		{
			if (Game.Instance.Player.IsDead)
			{
				// Restart the game directly (MainMenuUI has ProcessMode.Always)
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
			Game.Instance.WifeMode = false;
			StartNewGame();
		}
	}

	private void OnEasyPressed()
	{
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
		GameSettings.ShootingStyle = GameSettings.ShootingStyle == ShootingStyle.AutoAim
			? ShootingStyle.Directional
			: ShootingStyle.AutoAim;

		UpdateShootingStyleLabel();
	}

	private void UpdateShootingStyleLabel()
	{
		string styleName = GameSettings.ShootingStyle == ShootingStyle.AutoAim
			? "Auto-Aim"
			: "Directional";
		_shootingStyleLabel.Text = $"Aim: {styleName}";
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
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
			_easyButton.Visible = false;
			_startLabel.Text = Game.Instance.Player.IsDead ? "Restart" : "Resume";
		}
		else
		{
			_easyButton.Visible = true;
			_startLabel.Text = "Start";
		}
	}
}
