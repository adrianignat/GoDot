using Godot;

namespace dTopDownShooter.Scripts.UI
{
	public partial class ShelterMarker : Node2D
	{
		private Sprite2D _markerSprite;
		private Label _distanceLabel;
		private DayNightManager _dayNightManager;
		private float _pulseTime = 0f;
		private const float PulseSpeed = 3f;

		public override void _Ready()
		{
			// Create marker sprite (a simple circle/indicator)
			_markerSprite = new Sprite2D();
			AddChild(_markerSprite);
			CreateMarkerTexture();

			// Distance label
			_distanceLabel = new Label();
			_distanceLabel.Position = new Vector2(0, 50);
			_distanceLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_distanceLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_distanceLabel.AddThemeFontSizeOverride("font_size", 14);
			AddChild(_distanceLabel);

			// Connect to signals
			Game.Instance.ShelterWarning += OnShelterWarning;
			Game.Instance.DayStarted += OnDayStarted;
			Game.Instance.PlayerEnteredShelter += OnPlayerEnteredShelter;

			Visible = false;
			ZIndex = 10;
		}

		public void SetDayNightManager(DayNightManager manager)
		{
			_dayNightManager = manager;
		}

		private void CreateMarkerTexture()
		{
			// Create a simple arrow/marker texture procedurally
			var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			// Draw a simple down-pointing triangle marker
			var color = new Color(1, 0.8f, 0, 1); // Yellow/gold

			// Draw filled triangle pointing down
			for (int y = 0; y < 32; y++)
			{
				int halfWidth = y / 2;
				for (int x = 32 - halfWidth; x <= 32 + halfWidth; x++)
				{
					if (x >= 0 && x < 64)
						image.SetPixel(x, y, color);
				}
			}

			// Draw outline of circle at bottom
			for (int angle = 0; angle < 360; angle++)
			{
				float rad = Mathf.DegToRad(angle);
				int x = 32 + (int)(12 * Mathf.Cos(rad));
				int y = 44 + (int)(12 * Mathf.Sin(rad));
				if (x >= 0 && x < 64 && y >= 0 && y < 64)
					image.SetPixel(x, y, color);
			}

			var texture = ImageTexture.CreateFromImage(image);
			_markerSprite.Texture = texture;
		}

		public override void _Process(double delta)
		{
			if (!Visible || _dayNightManager == null)
				return;

			// Position at shelter location
			GlobalPosition = _dayNightManager.CurrentShelterPosition + new Vector2(0, -60);

			// Pulse animation
			_pulseTime += (float)delta * PulseSpeed;
			float scale = 1f + 0.2f * Mathf.Sin(_pulseTime);
			_markerSprite.Scale = new Vector2(scale, scale);

			// Update distance label
			var player = Game.Instance.Player;
			if (player != null)
			{
				float distance = player.GlobalPosition.DistanceTo(_dayNightManager.CurrentShelterPosition);
				_distanceLabel.Text = $"{(int)distance}m";

				// Change color based on distance
				if (distance < 100)
				{
					_markerSprite.Modulate = Colors.Green;
					_distanceLabel.AddThemeColorOverride("font_color", Colors.Green);
				}
				else if (distance < 200)
				{
					_markerSprite.Modulate = Colors.Yellow;
					_distanceLabel.AddThemeColorOverride("font_color", Colors.Yellow);
				}
				else
				{
					_markerSprite.Modulate = Colors.Red;
					_distanceLabel.AddThemeColorOverride("font_color", Colors.Red);
				}
			}
		}

		private void OnShelterWarning()
		{
			Visible = true;
			_pulseTime = 0f;
		}

		private void OnDayStarted(int dayNumber)
		{
			Visible = false;
		}

		private void OnPlayerEnteredShelter()
		{
			// Brief flash then hide
			_markerSprite.Modulate = Colors.Green;
		}
	}
}
