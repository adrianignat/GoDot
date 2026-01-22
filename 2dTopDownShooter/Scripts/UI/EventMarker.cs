using Godot;

namespace dTopDownShooter.Scripts.UI
{
	/// <summary>
	/// GPS-style directional arrow at the top of the screen that points towards a target.
	/// Shows the name and distance to the target.
	/// </summary>
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
			Layer = 50; // Above game, below menus

			// Create a container control for the GPS HUD
			_container = new Control();
			_container.SetAnchorsPreset(Control.LayoutPreset.TopWide);
			_container.CustomMinimumSize = new Vector2(0, 100);
			_container.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_container);

			// Create the arrow sprite (positioned at top center)
			_arrowSprite = new Sprite2D();
			_arrowSprite.Position = new Vector2(0, 60);
			CreateArrowTexture();
			_container.AddChild(_arrowSprite);

			// Name label (above arrow)
			_nameLabel = new Label();
			_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_nameLabel.AddThemeColorOverride("font_color", _markerColor);
			_nameLabel.AddThemeFontSizeOverride("font_size", 16);
			_nameLabel.AddThemeConstantOverride("outline_size", 3);
			_nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_nameLabel.Text = _markerName;
			_container.AddChild(_nameLabel);

			// Distance label (below arrow)
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
			// Create an arrow pointing down (default direction, will be rotated)
			var image = Image.CreateEmpty(32, 48, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			var color = Colors.White;

			// Draw arrow pointing down (triangle)
			for (int y = 0; y < 32; y++)
			{
				int halfWidth = y / 2;
				for (int x = 16 - halfWidth; x <= 16 + halfWidth; x++)
				{
					if (x >= 0 && x < 32)
						image.SetPixel(x, y, color);
				}
			}

			// Draw arrow shaft (rectangle going up)
			for (int y = 0; y < 20; y++)
			{
				for (int x = 12; x < 20; x++)
				{
					// Only draw shaft behind the triangle
					if (y < 8)
						image.SetPixel(x, y, color);
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

			// Get current target position
			Vector2 targetPos = GetTargetPosition();
			if (targetPos == Vector2.Zero)
				return;

			// Get screen size
			var viewport = GetViewport();
			var screenSize = viewport.GetVisibleRect().Size;
			float screenCenterX = screenSize.X / 2;

			// Position elements at top center
			_arrowSprite.Position = new Vector2(screenCenterX, 55);
			_nameLabel.Position = new Vector2(screenCenterX - 60, 5);
			_nameLabel.Size = new Vector2(120, 25);
			_distanceLabel.Position = new Vector2(screenCenterX - 40, 85);
			_distanceLabel.Size = new Vector2(80, 20);

			// Calculate direction from player to target
			Vector2 playerPos = player.GlobalPosition;
			Vector2 direction = (targetPos - playerPos).Normalized();
			float distance = playerPos.DistanceTo(targetPos);

			// Calculate angle: 0 = down, rotate based on direction
			// direction.Angle() gives angle from +X axis
			// We want angle from -Y axis (up), so add PI/2
			float angle = direction.Angle() + Mathf.Pi / 2;
			_arrowSprite.Rotation = angle;

			// Pulse animation for scale
			_pulseTime += (float)delta * PulseSpeed;
			float pulseScale = 1f + 0.1f * Mathf.Sin(_pulseTime);
			_arrowSprite.Scale = new Vector2(pulseScale, pulseScale);

			// Update distance label
			_distanceLabel.Text = $"{(int)distance}m";

			// Change color based on distance
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
			// Prefer dynamic node target if set and valid
			if (_targetNode != null && IsInstanceValid(_targetNode))
				return _targetNode.GlobalPosition;

			// Fall back to static position
			return _targetPosition ?? Vector2.Zero;
		}

		/// <summary>
		/// Set a static position as the target.
		/// </summary>
		public void SetTarget(Vector2 position)
		{
			_targetPosition = position;
			_targetNode = null;
		}

		/// <summary>
		/// Set a node as the target (will follow the node).
		/// </summary>
		public void SetTarget(Node2D node)
		{
			_targetNode = node;
			_targetPosition = null;
		}

		/// <summary>
		/// Configure the marker appearance.
		/// </summary>
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
			{
				_arrowSprite.Modulate = color;
			}

			if (_distanceLabel != null)
			{
				_distanceLabel.AddThemeColorOverride("font_color", color);
			}
		}

		/// <summary>
		/// Show the marker.
		/// </summary>
		public new void Show()
		{
			Visible = true;
			_pulseTime = 0f;
		}

		/// <summary>
		/// Hide the marker.
		/// </summary>
		public new void Hide()
		{
			Visible = false;
		}
	}
}
