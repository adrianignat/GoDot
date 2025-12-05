using Godot;

namespace dTopDownShooter.Scripts.UI
{
	public partial class DashIndicator : Control
	{
		private Player _player;
		private float _cooldownProgress = 1f; // 1 = ready, 0 = just used

		// Visual settings
		private readonly float _circleRadius = 25f;
		private readonly float _borderWidth = 3f;
		private readonly Color _readyColor = new Color(0.4f, 0.8f, 0.4f); // Green when ready
		private readonly Color _cooldownColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray background
		private readonly Color _fillColor = new Color(0.5f, 0.7f, 0.9f); // Light blue fill
		private readonly Color _arrowColor = Colors.White;
		private readonly Color _textColor = new Color(0.6f, 0.6f, 0.6f); // Pale gray

		public override void _Ready()
		{
			// Set minimum size for the control
			CustomMinimumSize = new Vector2(70, 80);
		}

		public override void _Process(double delta)
		{
			// Find player if not cached
			if (_player == null)
			{
				_player = Game.Instance?.Player;
			}

			if (_player == null) return;

			// Calculate cooldown progress (1 = ready, 0 = just used)
			float cooldownRemaining = _player.DashCooldownRemaining;
			if (cooldownRemaining <= 0)
			{
				_cooldownProgress = 1f;
			}
			else
			{
				// Assuming 15 second cooldown from Player.cs
				const float maxCooldown = 15f;
				_cooldownProgress = 1f - (cooldownRemaining / maxCooldown);
			}

			// Request redraw every frame
			QueueRedraw();
		}

		public override void _Draw()
		{
			Vector2 center = new Vector2(CustomMinimumSize.X / 2, _circleRadius + 5);

			// Draw background circle (dark gray)
			DrawCircle(center, _circleRadius, _cooldownColor);

			// Draw fill arc based on cooldown progress
			if (_cooldownProgress > 0 && _cooldownProgress < 1f)
			{
				DrawCooldownArc(center, _circleRadius - 2, _cooldownProgress, _fillColor);
			}
			else if (_cooldownProgress >= 1f)
			{
				// Fully ready - fill with ready color
				DrawCircle(center, _circleRadius - 2, _readyColor);
			}

			// Draw circle border
			DrawArc(center, _circleRadius, 0, Mathf.Tau, 32, _arrowColor, _borderWidth);

			// Draw arrow pointing right (direction of dash)
			DrawArrow(center, _arrowColor);

			// Draw "space" text below
			DrawSpaceText(center);
		}

		private void DrawCooldownArc(Vector2 center, float radius, float progress, Color color)
		{
			// Draw filled arc from top, going clockwise
			float startAngle = -Mathf.Pi / 2; // Start at top
			float endAngle = startAngle + (Mathf.Tau * progress);

			// Draw as filled polygon segments
			int segments = 32;
			var points = new Vector2[segments + 2];
			points[0] = center;

			for (int i = 0; i <= segments; i++)
			{
				float angle = startAngle + (endAngle - startAngle) * i / segments;
				points[i + 1] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			}

			// Only draw if we have enough points for a polygon
			if (progress > 0.01f)
			{
				int pointCount = (int)(segments * progress) + 2;
				if (pointCount > 2)
				{
					var drawPoints = new Vector2[pointCount];
					for (int i = 0; i < pointCount; i++)
					{
						drawPoints[i] = points[i];
					}
					DrawPolygon(drawPoints, new Color[] { color });
				}
			}
		}

		private void DrawArrow(Vector2 center, Color color)
		{
			float arrowSize = _circleRadius * 0.5f;

			// Arrow shaft
			Vector2 shaftStart = center - new Vector2(arrowSize * 0.6f, 0);
			Vector2 shaftEnd = center + new Vector2(arrowSize * 0.4f, 0);
			DrawLine(shaftStart, shaftEnd, color, 3f);

			// Arrow head
			Vector2 headTip = center + new Vector2(arrowSize * 0.7f, 0);
			Vector2 headTop = center + new Vector2(arrowSize * 0.2f, -arrowSize * 0.4f);
			Vector2 headBottom = center + new Vector2(arrowSize * 0.2f, arrowSize * 0.4f);

			DrawLine(headTip, headTop, color, 3f);
			DrawLine(headTip, headBottom, color, 3f);
		}

		private void DrawSpaceText(Vector2 circleCenter)
		{
			var font = ThemeDB.FallbackFont;
			int fontSize = 14;
			string text = "space";

			Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
			Vector2 textPos = new Vector2(
				circleCenter.X - textSize.X / 2,
				circleCenter.Y + _circleRadius + 18
			);

			DrawString(font, textPos, text, HorizontalAlignment.Left, -1, fontSize, _textColor);
		}
	}
}
