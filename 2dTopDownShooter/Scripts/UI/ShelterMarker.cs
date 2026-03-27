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
			Game.Instance.NightStarted += OnNightStarted;
			Game.Instance.PlayerEnteredShelter += OnPlayerEnteredShelter;

			Visible = false;
		}

		public void SetDayNightManager(DayNightManager manager)
		{
			_dayNightManager = manager;
		}

		private void CreateArrowTexture()
		{
			const int width = 52;
			const int height = 52;
			var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			Color darkOutline = new Color(0.24f, 0.14f, 0.04f, 0.98f);
			Color leftShade = new Color(0.85f, 0.64f, 0.18f, 1f);
			Color rightShade = new Color(1f, 0.95f, 0.60f, 1f);
			Color centerHighlight = new Color(1f, 0.99f, 0.82f, 0.98f);

			Vector2[] pointer =
			{
				new Vector2(width * 0.5f, 2),
				new Vector2(width - 4, height - 10),
				new Vector2(width * 0.5f, height - 18),
				new Vector2(4, height - 10)
			};

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var point = new Vector2(x + 0.5f, y + 0.5f);
					if (!Geometry2D.IsPointInPolygon(point, pointer))
						continue;

					bool border = false;
					for (int oy = -1; oy <= 1 && !border; oy++)
					{
						for (int ox = -1; ox <= 1; ox++)
						{
							var sample = new Vector2(x + ox + 0.5f, y + oy + 0.5f);
							if (!Geometry2D.IsPointInPolygon(sample, pointer))
							{
								border = true;
								break;
							}
						}
					}

					Color fill = x < width / 2 ? leftShade : rightShade;
					if (Mathf.Abs(x - width / 2f) < 3)
						fill = centerHighlight;

					image.SetPixel(x, y, border ? darkOutline : fill);
				}
			}

			for (int y = height - 18; y < height - 8; y++)
			{
				int inset = (y - (height - 18)) / 2 + 1;
				for (int x = width / 2 - 5 + inset; x <= width / 2 + 5 - inset; x++)
					image.SetPixel(x, y, new Color(0, 0, 0, 0));
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

		private void OnNightStarted()
		{
			Visible = true;
			_pulseTime = 0f;
		}

		private void OnPlayerEnteredShelter()
		{
			_arrowSprite.Modulate = Colors.Green;
		}
	}
}

