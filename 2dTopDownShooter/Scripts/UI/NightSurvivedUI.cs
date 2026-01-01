using Godot;

namespace dTopDownShooter.Scripts.UI
{
	/// <summary>
	/// UI displayed when the player survives the night, offering choices to continue or restart.
	/// </summary>
	public partial class NightSurvivedUI : CanvasLayer
	{
		private Label _titleLabel;
		private Label _subtitleLabel;
		private Button _continueButton;
		private Button _newGameButton;
		private ColorRect _background;
		private Control _container;

		[Signal]
		public delegate void ContinueSelectedEventHandler();

		[Signal]
		public delegate void NewGameSelectedEventHandler();

		public override void _Ready()
		{
			// UI must process during pause to handle button clicks
			ProcessMode = ProcessModeEnum.Always;

			Layer = 90; // Below fade overlay but above game

			CreateUI();
			Hide();
		}

		private void CreateUI()
		{
			// Semi-transparent dark background
			_background = new ColorRect();
			_background.Color = new Color(0, 0, 0, 0.85f);
			_background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_background.MouseFilter = Control.MouseFilterEnum.Stop;
			AddChild(_background);

			// Center container
			_container = new Control();
			_container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			AddChild(_container);

			// VBox for content
			var vbox = new VBoxContainer();
			vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
			vbox.GrowHorizontal = Control.GrowDirection.Both;
			vbox.GrowVertical = Control.GrowDirection.Both;
			vbox.AddThemeConstantOverride("separation", 20);
			_container.AddChild(vbox);

			// Title
			_titleLabel = new Label();
			_titleLabel.Text = "CONGRATULATIONS";
			_titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_titleLabel.AddThemeFontSizeOverride("font_size", 48);
			_titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.2f)); // Gold color
			vbox.AddChild(_titleLabel);

			// Subtitle
			_subtitleLabel = new Label();
			_subtitleLabel.Text = "You have become the Night Walker";
			_subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_subtitleLabel.AddThemeFontSizeOverride("font_size", 28);
			_subtitleLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 1f)); // Light blue
			vbox.AddChild(_subtitleLabel);

			// Spacer
			var spacer = new Control();
			spacer.CustomMinimumSize = new Vector2(0, 30);
			vbox.AddChild(spacer);

			// Question
			var questionLabel = new Label();
			questionLabel.Text = "Do you wish to continue or start a new game?";
			questionLabel.HorizontalAlignment = HorizontalAlignment.Center;
			questionLabel.AddThemeFontSizeOverride("font_size", 22);
			vbox.AddChild(questionLabel);

			// Button container
			var buttonContainer = new HBoxContainer();
			buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;
			buttonContainer.AddThemeConstantOverride("separation", 40);
			vbox.AddChild(buttonContainer);

			// Continue button
			_continueButton = new Button();
			_continueButton.Text = "Continue";
			_continueButton.CustomMinimumSize = new Vector2(200, 60);
			_continueButton.AddThemeFontSizeOverride("font_size", 28);
			_continueButton.Pressed += OnContinuePressed;
			buttonContainer.AddChild(_continueButton);

			// New Game button
			_newGameButton = new Button();
			_newGameButton.Text = "New Game";
			_newGameButton.CustomMinimumSize = new Vector2(200, 60);
			_newGameButton.AddThemeFontSizeOverride("font_size", 28);
			_newGameButton.Pressed += OnNewGamePressed;
			buttonContainer.AddChild(_newGameButton);
		}

		public void ShowNightSurvived(int dayCompleted)
		{
			_subtitleLabel.Text = $"You have become the Night Walker\nDay {dayCompleted} survived!";
			Show();
			Game.Instance.IsPaused = true;
		}

		private void OnContinuePressed()
		{
			Hide();
			Game.Instance.IsPaused = false;
			EmitSignal(SignalName.ContinueSelected);
		}

		private void OnNewGamePressed()
		{
			Hide();
			Game.Instance.IsPaused = false;
			EmitSignal(SignalName.NewGameSelected);
		}
	}
}
