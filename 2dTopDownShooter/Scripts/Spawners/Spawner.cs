using Godot;

namespace dTopDownShooter.Scripts.Spawners
{
	/// <summary>
	/// Generic base class for spawning objects at timed intervals.
	/// Override <see cref="GetLocation"/> to customize spawn position,
	/// <see cref="CanSpawn"/> to add spawn conditions, and
	/// <see cref="InitializeSpawnedObject"/> to configure objects before they enter the scene.
	/// </summary>
	/// <typeparam name="T">The type of Node2D to spawn</typeparam>
	public partial class Spawner<T> : Node2D where T : Node2D
	{
		[Export]
		public PackedScene Scene;

		/// <summary>
		/// Number of objects to spawn per second. Set to 0 to disable spawning.
		/// </summary>
		[Export]
		public float ObjectsPerSecond;

		/// <summary>
		/// Maximum number of objects this spawner will create. Set to 0 for unlimited.
		/// </summary>
		[Export]
		public ushort MaxObjects;

		private float spawnRate => 1 / ObjectsPerSecond;
		private float timeUntilNextSpawn = 0f;
		private ushort spawnedObjects = 0;

		/// <summary>
		/// Returns the world position where the next object should spawn.
		/// Override to customize spawn location.
		/// </summary>
		public virtual Vector2 GetLocation()
		{
			return GlobalPosition;
		}

		/// <summary>
		/// Returns whether spawning is currently allowed.
		/// Override to add custom spawn conditions.
		/// </summary>
		public virtual bool CanSpawn()
		{
			return MaxObjects == 0 || MaxObjects >= spawnedObjects;
		}

		/// <summary>
		/// Returns the parent node where spawned objects should be added.
		/// Override to use EntityLayer for Y-sorted objects.
		/// Default adds to scene root.
		/// </summary>
		protected virtual Node GetSpawnParent()
		{
			return GetTree().Root;
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
			InitializeSpawnedObject(spawnedObject);
			GetSpawnParent().AddChild(spawnedObject);

			if (MaxObjects != 0)
				spawnedObjects++;
		}

		/// <summary>
		/// Override this method to initialize spawned objects with custom properties
		/// before they are added to the scene tree.
		/// </summary>
		protected virtual void InitializeSpawnedObject(T spawnedObject)
		{
		}

	}
}
