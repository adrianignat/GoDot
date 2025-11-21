using Godot;

namespace dTopDownShooter.Scripts.Spawners
{
	public partial class Spawner<T> : Node2D where T : Node2D
	{
		[Export]
		public PackedScene Scene;

		[Export]
		public float ObjectsPerSecond;

		[Export]
		public ushort MaxObjects;

		private float spawnRate => 1 / ObjectsPerSecond;
		private float timeUntilNextSpawn = 0f;
		private ushort spawnedObjects = 0;

		public virtual Vector2 GetLocation()
		{
			return GlobalPosition;
		}

		public virtual bool CanSpawn()
		{
			return MaxObjects == 0 || MaxObjects >= spawnedObjects;
		}

		public override void _Process(double delta)
		{
			if (Game.Instance.IsPaused)
                return;

			if (timeUntilNextSpawn > spawnRate && CanSpawn())
			{
				timeUntilNextSpawn = 0f;
				Spawn();
			}
			else
			{
				timeUntilNextSpawn += (float)delta;
			}
		}

		protected virtual void Spawn()
		{
			var spawnedObject = Scene.Instantiate<T>();
			spawnedObject.GlobalPosition = GetLocation();
			GetTree().Root.AddChild(spawnedObject);

			if (MaxObjects != 0)
				spawnedObjects++;
		}

	}
}
