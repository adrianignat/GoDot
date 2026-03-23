using Godot;

namespace dTopDownShooter.Scripts.UI
{
	public partial class EventMarker : CanvasLayer
	{
		private Control _container;
		private Sprite2D _arrowSprite;
		private Label _nameLabel;
		private Label _distanceLabel;
		private float _pulseTime = 0f;
		private const float PulseSpeed = 2f;

		private Vector2? _targetPosition;
		private Node2D _targetNode;
		private Color _markerColor = Colors.Cyan;
		private string _markerName = "Objective";

		public override void _Ready()
		{
			Layer = 50;

			_container = new Control();
			_container.SetAnchorsPreset(Control.LayoutPreset.TopWide);
			_container.CustomMinimumSize = new Vector2(0, 100);
			_container.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_container);

			_arrowSprite = new Sprite2D();
			_arrowSprite.Position = new Vector2(0, 60);
			CreateArrowTexture();
			_container.AddChild(_arrowSprite);

			_nameLabel = new Label();
			_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_nameLabel.AddThemeColorOverride("font_color", _markerColor);
			_nameLabel.AddThemeFontSizeOverride("font_size", 16);
			_nameLabel.AddThemeConstantOverride("outline_size", 3);
			_nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_nameLabel.Text = _markerName;
			_container.AddChild(_nameLabel);

			_distanceLabel = new Label();
			_distanceLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_distanceLabel.AddThemeColorOverride("font_color", _markerColor);
			_distanceLabel.AddThemeFontSizeOverride("font_size", 14);
			_distanceLabel.AddThemeConstantOverride("outline_size", 2);
			_distanceLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_container.AddChild(_distanceLabel);

			Visible = false;
		}

		private void CreateArrowTexture()
		{
			const int width = 48;
			const int height = 64;
			var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			Color baseColor = new Color(0.90f, 0.95f, 1f, 1f);
			Color leftShade = new Color(0.62f, 0.72f, 0.82f, 1f);
			Color rightShade = new Color(0.97f, 0.99f, 1f, 1f);
			Color darkOutline = new Color(0.12f, 0.16f, 0.22f, 0.95f);
			Color fletching = new Color(0.47f, 0.26f, 0.12f, 1f);

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

			var texture = ImageTexture.CreateFromImage(image);
			_arrowSprite.Texture = texture;
			_arrowSprite.Modulate = _markerColor;
		}

		public override void _Process(double delta)
		{
			if (!Visible)
				return;

			var player = Game.Instance.Player;
			if (player == null)
				return;

			Vector2 targetPos = GetTargetPosition();
			if (targetPos == Vector2.Zero)
				return;

			var viewport = GetViewport();
			var screenSize = viewport.GetVisibleRect().Size;
			float screenCenterX = screenSize.X / 2;

			_arrowSprite.Position = new Vector2(screenCenterX, 55);
			_nameLabel.Position = new Vector2(screenCenterX - 80, 4);
			_nameLabel.Size = new Vector2(160, 25);
			_distanceLabel.Position = new Vector2(screenCenterX - 50, 88);
			_distanceLabel.Size = new Vector2(100, 20);

			Vector2 playerPos = player.GlobalPosition;
			Vector2 direction = (targetPos - playerPos).Normalized();
			float distance = playerPos.DistanceTo(targetPos);
			float angle = direction.Angle() + Mathf.Pi / 2;
			_arrowSprite.Rotation = angle;

			_pulseTime += (float)delta * PulseSpeed;
			float pulseScale = 1f + 0.08f * Mathf.Sin(_pulseTime);
			_arrowSprite.Scale = new Vector2(pulseScale, pulseScale);

			_distanceLabel.Text = $"{(int)distance}m";

			Color distanceColor;
			if (distance < 150)
				distanceColor = Colors.Green;
			else if (distance < 400)
				distanceColor = Colors.Yellow;
			else
				distanceColor = _markerColor;

			_arrowSprite.Modulate = distanceColor;
			_distanceLabel.AddThemeColorOverride("font_color", distanceColor);
		}

		private Vector2 GetTargetPosition()
		{
			if (_targetNode != null && IsInstanceValid(_targetNode))
				return _targetNode.GlobalPosition;

			return _targetPosition ?? Vector2.Zero;
		}

		public void SetTarget(Vector2 position)
		{
			_targetPosition = position;
			_targetNode = null;
		}

		public void SetTarget(Node2D node)
		{
			_targetNode = node;
			_targetPosition = null;
		}

		public void Configure(string name, Color color)
		{
			_markerName = name;
			_markerColor = color;

			if (_nameLabel != null)
			{
				_nameLabel.Text = name;
				_nameLabel.AddThemeColorOverride("font_color", color);
			}

			if (_arrowSprite != null)
				_arrowSprite.Modulate = color;

			if (_distanceLabel != null)
				_distanceLabel.AddThemeColorOverride("font_color", color);
		}

		public new void Show()
		{
			Visible = true;
			_pulseTime = 0f;
		}

		public new void Hide()
		{
			Visible = false;
		}
	}
}
