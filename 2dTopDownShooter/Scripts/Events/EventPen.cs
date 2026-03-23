using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class EventPen : Node2D
	{
		public override void _Ready()
		{
			ZIndex = 1;
			AddChild(CreateFenceShadow());
			AddChild(CreateFenceFrame());
			AddChild(CreateGround());
		}

		private Node2D CreateFenceFrame()
		{
			var root = new Node2D();
			Color fence = new Color(0.63f, 0.41f, 0.20f);
			Color post = new Color(0.44f, 0.26f, 0.10f);

			root.AddChild(CreateRail(new Vector2(0, -58), new Vector2(152, 14), fence));
			root.AddChild(CreateRail(new Vector2(0, 58), new Vector2(152, 14), fence));
			root.AddChild(CreateRail(new Vector2(-74, 0), new Vector2(14, 132), fence));
			root.AddChild(CreateRail(new Vector2(74, 0), new Vector2(14, 132), fence));

			foreach (var pos in new[] { new Vector2(-74, -58), new Vector2(74, -58), new Vector2(-74, 58), new Vector2(74, 58) })
				root.AddChild(CreatePost(pos, post));

			return root;
		}

		private Node2D CreateFenceShadow()
		{
			var root = new Node2D();
			root.Modulate = new Color(0, 0, 0, 0.18f);
			root.Position = new Vector2(8, 10);
			root.AddChild(CreateRail(new Vector2(0, -58), new Vector2(152, 14), Colors.Black));
			root.AddChild(CreateRail(new Vector2(0, 58), new Vector2(152, 14), Colors.Black));
			root.AddChild(CreateRail(new Vector2(-74, 0), new Vector2(14, 132), Colors.Black));
			root.AddChild(CreateRail(new Vector2(74, 0), new Vector2(14, 132), Colors.Black));
			return root;
		}

		private Node2D CreateGround()
		{
			var poly = new Polygon2D();
			poly.Color = new Color(0.88f, 0.69f, 0.85f, 0.22f);
			poly.Polygon = new[]
			{
				new Vector2(-58, -42),
				new Vector2(58, -42),
				new Vector2(58, 42),
				new Vector2(-58, 42)
			};
			return poly;
		}

		private static ColorRect CreateRail(Vector2 pos, Vector2 size, Color color)
		{
			var rect = new ColorRect();
			rect.Color = color;
			rect.Position = pos - size / 2f;
			rect.Size = size;
			return rect;
		}

		private static ColorRect CreatePost(Vector2 pos, Color color)
		{
			var rect = new ColorRect();
			rect.Color = color;
			rect.Position = pos - new Vector2(8, 8);
			rect.Size = new Vector2(16, 16);
			return rect;
		}
	}
}
