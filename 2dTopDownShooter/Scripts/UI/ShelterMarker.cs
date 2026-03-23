using Godot;

namespace dTopDownShooter.Scripts.UI
{
	public partial class ShelterMarker : CanvasLayer
	{
		private Control _container;
		private Sprite2D _arrowSprite;
		private Label _nameLabel;
		private Label _distanceLabel;
		private DayNightManager _dayNightManager;
		private float _pulseTime = 0f;
		private const float PulseSpeed = 2.5f;

		public override void _Ready()
		{
			Layer = 55;

			_container = new Control();
			_container.SetAnchorsPreset(Control.LayoutPreset.TopWide);
			_container.CustomMinimumSize = new Vector2(0, 110);
			_container.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_container);

			_arrowSprite = new Sprite2D();
			CreateArrowTexture();
			_container.AddChild(_arrowSprite);

			_nameLabel = new Label();
			_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_nameLabel.AddThemeColorOverride("font_color", Colors.Gold);
			_nameLabel.AddThemeFontSizeOverride("font_size", 17);
			_nameLabel.AddThemeConstantOverride("outline_size", 3);
			_nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_nameLabel.Text = "Shelter";
			_container.AddChild(_nameLabel);

			_distanceLabel = new Label();
			_distanceLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_distanceLabel.AddThemeColorOverride("font_color", Colors.Gold);
			_distanceLabel.AddThemeFontSizeOverride("font_size", 14);
			_distanceLabel.AddThemeConstantOverride("outline_size", 2);
			_distanceLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_container.AddChild(_distanceLabel);

			Game.Instance.ShelterWarning += OnShelterWarning;
			Game.Instance.DayStarted += OnDayStarted;
			Game.Instance.PlayerEnteredShelter += OnPlayerEnteredShelter;

			Visible = false;
		}

		public void SetDayNightManager(DayNightManager manager)
		{
			_dayNightManager = manager;
		}

		private void CreateArrowTexture()
		{
			const int width = 48;
			const int height = 64;
			var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			Color baseColor = new Color(1f, 0.90f, 0.45f, 1f);
			Color leftShade = new Color(0.86f, 0.62f, 0.16f, 1f);
			Color rightShade = new Color(1f, 0.98f, 0.72f, 1f);
			Color darkOutline = new Color(0.24f, 0.14f, 0.04f, 0.98f);
			Color fletching = new Color(0.55f, 0.20f, 0.08f, 1f);

			for (int y = 0; y <= 28; y++)
			{
				float t = y / 28.0f;
				int halfWidth = Mathf.RoundToInt(Mathf.Lerp(2, 18, t));
				int centerX = width / 2;
				for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
				{
					if (x < 0 || x >= width)
						continue;

					Color color = x < centerX ? leftShade : rightShade;
					if (Mathf.Abs(x - centerX) <= 1)
						color = baseColor;

					bool edge = x == centerX - halfWidth || x == centerX + halfWidth || y == 0 || y == 28;
					image.SetPixel(x, y, edge ? darkOutline : color);
				}
			}

			for (int y = 20; y <= 56; y++)
			{
				for (int x = 21; x <= 27; x++)
				{
					Color color = x <= 23 ? leftShade : rightShade;
					if (x == 24)
						color = baseColor;
					bool edge = x == 21 || x == 27;
					image.SetPixel(x, y, edge ? darkOutline : color);
				}
			}

			for (int y = 44; y <= 60; y++)
			{
				int spread = y - 44;
				for (int x = 24 - spread; x <= 24 - Mathf.Max(1, spread / 2); x++)
				{
					if (x >= 0 && x < width)
						image.SetPixel(x, y, x == 24 - spread ? darkOutline : fletching);
				}
				for (int x = 24 + Mathf.Max(1, spread / 2); x <= 24 + spread; x++)
				{
					if (x >= 0 && x < width)
						image.SetPixel(x, y, x == 24 + spread ? darkOutline : fletching);
				}
			}

			_arrowSprite.Texture = ImageTexture.CreateFromImage(image);
			_arrowSprite.Modulate = Colors.Gold;
		}

		public override void _Process(double delta)
		{
			if (!Visible || _dayNightManager == null)
				return;

			var player = Game.Instance.Player;
			if (player == null)
				return;

			Vector2 shelterPosition = _dayNightManager.CurrentShelterPosition;
			Vector2 direction = (shelterPosition - player.GlobalPosition).Normalized();
			float distance = player.GlobalPosition.DistanceTo(shelterPosition);
			float angle = direction.Angle() + Mathf.Pi / 2;

			var screenSize = GetViewport().GetVisibleRect().Size;
			float screenCenterX = screenSize.X / 2;
			_arrowSprite.Position = new Vector2(screenCenterX, 55);
			_nameLabel.Position = new Vector2(screenCenterX - 80, 5);
			_nameLabel.Size = new Vector2(160, 25);
			_distanceLabel.Position = new Vector2(screenCenterX - 50, 88);
			_distanceLabel.Size = new Vector2(100, 20);

			_arrowSprite.Rotation = angle;
			_pulseTime += (float)delta * PulseSpeed;
			float scale = 1f + 0.08f * Mathf.Sin(_pulseTime);
			_arrowSprite.Scale = new Vector2(scale, scale);
			_distanceLabel.Text = $"{(int)distance}m";

			if (distance < 100)
			{
				_arrowSprite.Modulate = Colors.Green;
				_distanceLabel.AddThemeColorOverride("font_color", Colors.Green);
			}
			else if (distance < 250)
			{
				_arrowSprite.Modulate = Colors.Yellow;
				_distanceLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			}
			else
			{
				_arrowSprite.Modulate = Colors.Gold;
				_distanceLabel.AddThemeColorOverride("font_color", Colors.Gold);
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
			_arrowSprite.Modulate = Colors.Gold;
		}

		private void OnPlayerEnteredShelter()
		{
			_arrowSprite.Modulate = Colors.Green;
		}
	}
}
