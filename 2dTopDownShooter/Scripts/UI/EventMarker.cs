using Godot;
using System.Collections.Generic;
using System.Linq;

namespace dTopDownShooter.Scripts.UI
{
	public partial class EventMarker : CanvasLayer
	{
		private static readonly HashSet<int> OccupiedSlots = new();
		private const float SlotHeight = 146f;
		private const float TopMargin = 4f;

		private Control _container;
		private Sprite2D _arrowSprite;
		private Label _nameLabel;
		private Label _statusLabel;
		private ProgressBar _progressBar;
		private Label _timerLabel;
		private Label _distanceLabel;
		private float _pulseTime = 0f;
		private const float PulseSpeed = 2f;

		private Vector2? _targetPosition;
		private Node2D _targetNode;
		private Color _markerColor = Colors.Cyan;
		private string _markerName = "Objective";
		private string _statusText = string.Empty;
		private string _timerText = string.Empty;
		private bool _navigationVisible = true;
		private int _slotIndex = -1;

		public override void _Ready()
		{
			Layer = 50;

			_container = new Control();
			_container.SetAnchorsPreset(Control.LayoutPreset.TopWide);
			_container.CustomMinimumSize = new Vector2(0, 138);
			_container.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_container);

			_arrowSprite = new Sprite2D();
			_arrowSprite.Position = new Vector2(0, 78);
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

			_statusLabel = new Label();
			_statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_statusLabel.AddThemeColorOverride("font_color", Colors.White);
			_statusLabel.AddThemeFontSizeOverride("font_size", 13);
			_statusLabel.AddThemeConstantOverride("outline_size", 2);
			_statusLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_statusLabel.Visible = false;
			_container.AddChild(_statusLabel);

			_progressBar = new ProgressBar();
			_progressBar.MinValue = 0;
			_progressBar.MaxValue = 1;
			_progressBar.Value = 0;
			_progressBar.ShowPercentage = false;
			_progressBar.Visible = false;
			_progressBar.Modulate = _markerColor;
			_container.AddChild(_progressBar);

			_timerLabel = new Label();
			_timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_timerLabel.AddThemeColorOverride("font_color", Colors.LightSalmon);
			_timerLabel.AddThemeFontSizeOverride("font_size", 13);
			_timerLabel.AddThemeConstantOverride("outline_size", 2);
			_timerLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_timerLabel.Visible = false;
			_container.AddChild(_timerLabel);

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
			const int width = 52;
			const int height = 52;
			var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0));

			Color darkOutline = new Color(0.10f, 0.14f, 0.20f, 0.95f);
			Color leftShade = new Color(0.63f, 0.74f, 0.86f, 1f);
			Color rightShade = new Color(0.92f, 0.97f, 1f, 1f);
			Color centerHighlight = new Color(1f, 1f, 1f, 0.95f);

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

			var texture = ImageTexture.CreateFromImage(image);
			_arrowSprite.Texture = texture;
			_arrowSprite.Modulate = _markerColor;
		}

		public override void _Process(double delta)
		{
			if (!Visible)
				return;

			var viewport = GetViewport();
			var screenSize = viewport.GetVisibleRect().Size;
			float screenCenterX = screenSize.X / 2;
			float slotOffsetY = TopMargin + (_slotIndex < 0 ? 0f : _slotIndex * SlotHeight);

			_nameLabel.Position = new Vector2(screenCenterX - 120, slotOffsetY + 4);
			_nameLabel.Size = new Vector2(240, 22);
			_statusLabel.Position = new Vector2(screenCenterX - 160, slotOffsetY + 25);
			_statusLabel.Size = new Vector2(320, 20);
			_progressBar.Position = new Vector2(screenCenterX - 95, slotOffsetY + 48);
			_progressBar.Size = new Vector2(190, 14);
			_timerLabel.Position = new Vector2(screenCenterX - 75, slotOffsetY + 66);
			_timerLabel.Size = new Vector2(150, 20);

			if (!_navigationVisible)
			{
				_arrowSprite.Visible = false;
				_distanceLabel.Visible = false;
				return;
			}

			var player = Game.Instance.Player;
			if (player == null)
				return;

			Vector2 targetPos = GetTargetPosition();
			if (targetPos == Vector2.Zero)
			{
				_arrowSprite.Visible = false;
				_distanceLabel.Visible = false;
				return;
			}

			_arrowSprite.Visible = true;
			_distanceLabel.Visible = true;
			_arrowSprite.Position = new Vector2(screenCenterX, slotOffsetY + 84);
			_distanceLabel.Position = new Vector2(screenCenterX - 60, slotOffsetY + 110);
			_distanceLabel.Size = new Vector2(120, 20);

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

		private int AcquireSlot()
		{
			int slot = 0;
			while (OccupiedSlots.Contains(slot))
				slot++;
			OccupiedSlots.Add(slot);
			return slot;
		}

		private void ReleaseSlot()
		{
			if (_slotIndex < 0)
				return;
			OccupiedSlots.Remove(_slotIndex);
			_slotIndex = -1;
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

		public void SetNavigationVisible(bool visible)
		{
			_navigationVisible = visible;
			if (_arrowSprite != null)
				_arrowSprite.Visible = visible;
			if (_distanceLabel != null)
				_distanceLabel.Visible = visible;
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

			if (_progressBar != null)
				_progressBar.Modulate = color;
		}

		public void SetStatus(string status)
		{
			_statusText = status ?? string.Empty;
			if (_statusLabel == null)
				return;

			_statusLabel.Text = _statusText;
			_statusLabel.Visible = !string.IsNullOrWhiteSpace(_statusText);
		}

		public void SetTimer(string timerText)
		{
			_timerText = timerText ?? string.Empty;
			if (_timerLabel == null)
				return;

			_timerLabel.Text = _timerText;
			_timerLabel.Visible = !string.IsNullOrWhiteSpace(_timerText);
		}

		public void SetProgress(float value, float maxValue)
		{
			if (_progressBar == null)
				return;

			if (maxValue <= 0f)
			{
				_progressBar.Visible = false;
				return;
			}

			_progressBar.MaxValue = maxValue;
			_progressBar.Value = Mathf.Clamp(value, 0f, maxValue);
			_progressBar.Visible = true;
		}

		public void ClearProgress()
		{
			if (_progressBar == null)
				return;

			_progressBar.Visible = false;
			_progressBar.Value = 0;
			_progressBar.MaxValue = 1;
		}

		public new void Show()
		{
			if (_slotIndex < 0)
				_slotIndex = AcquireSlot();
			Visible = true;
			_pulseTime = 0f;
			SetStatus(_statusText);
			SetTimer(_timerText);
		}

		public new void Hide()
		{
			Visible = false;
			ReleaseSlot();
		}

		public override void _ExitTree()
		{
			ReleaseSlot();
			base._ExitTree();
		}
	}
}
