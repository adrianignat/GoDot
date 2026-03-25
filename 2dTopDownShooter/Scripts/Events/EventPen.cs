using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class EventPen : Node2D
	{
		private static readonly Texture2D WoodTexture = GD.Load<Texture2D>("res://Sprites/Resources/Resources/W_Idle_(NoShadow).png");
		private static readonly Texture2D GrassTexture = GD.Load<Texture2D>("res://Sprites/Decor/Sheep/Sheep_Grass.png");

		public override void _Ready()
		{
			ZIndex = 1;
			AddChild(CreateGroundSprite());
			CreateWoodPerimeter();
		}

		private Node2D CreateGroundSprite()
		{
			var sprite = new Sprite2D();
			sprite.Texture = GrassTexture;
			sprite.Position = new Vector2(0, 18);
			sprite.Scale = new Vector2(1.35f, 1.2f);
			sprite.Modulate = new Color(1f, 0.92f, 1f, 0.95f);
			return sprite;
		}

		private void CreateWoodPerimeter()
		{
			if (WoodTexture == null)
				return;

			Vector2[] perimeter =
			{
				new Vector2(-82, -56),
				new Vector2(0, -70),
				new Vector2(82, -56),
				new Vector2(-96, 6),
				new Vector2(96, 6),
				new Vector2(-82, 68),
				new Vector2(0, 82),
				new Vector2(82, 68)
			};

			float[] rotations =
			{
				-0.22f,
				0f,
				0.22f,
				-Mathf.Pi / 2f,
				Mathf.Pi / 2f,
				0.22f,
				0f,
				-0.22f
			};

			for (int i = 0; i < perimeter.Length; i++)
				AddChild(CreateWoodPiece(perimeter[i], rotations[i]));
		}

		private Node2D CreateWoodPiece(Vector2 position, float rotation)
		{
			var sprite = new Sprite2D();
			sprite.Texture = WoodTexture;
			sprite.Position = position;
			sprite.Rotation = rotation;
			sprite.Scale = new Vector2(0.92f, 0.92f);
			return sprite;
		}
	}
}
