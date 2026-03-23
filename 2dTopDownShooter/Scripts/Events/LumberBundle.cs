using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class LumberBundle : Area2D
	{
		[Signal]
		public delegate void CollectedEventHandler(LumberBundle bundle);

		public bool IsCollected { get; private set; }

		public override void _Ready()
		{
			CollisionLayer = 0;
			CollisionMask = 0;

			var shape = new CollisionShape2D();
			shape.Shape = new CircleShape2D { Radius = 22f };
			AddChild(shape);

			var polygon = new Polygon2D();
			polygon.Color = new Color(0.55f, 0.32f, 0.12f);
			polygon.Polygon = new Vector2[]
			{
				new(-20, -12),
				new(22, -8),
				new(18, 14),
				new(-24, 10)
			};
			AddChild(polygon);
		}

		public void Collect()
		{
			if (IsCollected)
				return;

			IsCollected = true;
			EmitSignal(SignalName.Collected, this);
			QueueFree();
		}
	}
}
