using Godot;

namespace dTopDownShooter.Scripts.Events
{
	public partial class LumberBundle : Area2D
	{
		[Signal]
		public delegate void CollectedEventHandler(LumberBundle bundle);

		private const int FrameSize = 128;
		private const float DisplayScale = 0.55f;

		public bool IsCollected { get; private set; }

		private AnimatedSprite2D _sprite;

		public override void _Ready()
		{
			CollisionLayer = 0;
			CollisionMask = 0;

			var shape = new CollisionShape2D();
			shape.Position = new Vector2(0, 10);
			shape.Shape = new RectangleShape2D { Size = new Vector2(42, 32) };
			AddChild(shape);

			_sprite = new AnimatedSprite2D();
			_sprite.Position = new Vector2(0, -8);
			_sprite.Scale = new Vector2(DisplayScale, DisplayScale);
			_sprite.SpriteFrames = BuildFrames();
			_sprite.Play("spawn");
			_sprite.AnimationFinished += OnAnimationFinished;
			AddChild(_sprite);
		}

		private SpriteFrames BuildFrames()
		{
			var frames = new SpriteFrames();
			var spawnTexture = GD.Load<Texture2D>("res://Sprites/Resources/Resources/W_Spawn.png");
			var idleTexture = GD.Load<Texture2D>("res://Sprites/Resources/Resources/W_Idle.png");

			if (spawnTexture != null)
				BuildAnimation(frames, spawnTexture, "spawn", false, 10f);
			if (idleTexture != null)
				BuildAnimation(frames, idleTexture, "idle", true, 8f);

			return frames;
		}

		private static void BuildAnimation(SpriteFrames frames, Texture2D texture, string name, bool loop, float speed)
		{
			frames.AddAnimation(name);
			frames.SetAnimationLoop(name, loop);
			frames.SetAnimationSpeed(name, speed);

			int frameCount = texture.GetWidth() / FrameSize;
			for (int i = 0; i < frameCount; i++)
			{
				var atlas = new AtlasTexture();
				atlas.Atlas = texture;
				atlas.Region = new Rect2(i * FrameSize, 0, FrameSize, FrameSize);
				frames.AddFrame(name, atlas);
			}
		}

		private void OnAnimationFinished()
		{
			if (_sprite.Animation == "spawn")
				_sprite.Play("idle");
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


