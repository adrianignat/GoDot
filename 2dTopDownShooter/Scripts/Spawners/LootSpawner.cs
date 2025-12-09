using Godot;
using System;

namespace dTopDownShooter.Scripts.Spawners
{
	public partial class LootSpawner : Node2D
	{
		[Export]
		public PackedScene XPScene;

		[Export]
		public PackedScene GoldScene;

		public override void _Ready()
		{
			Game.Instance.EnemyKilled += OnEnemyKilled;
		}

		private void OnEnemyKilled(Enemy enemy)
		{
			Spawn(enemy.GlobalPosition);
		}

		public void Spawn(Vector2 position)
		{
			var gold = GoldScene.Instantiate<Gold>();
			gold.Position = position;
			Game.Instance.EntityLayer.CallDeferred("add_child", gold);
		}
	}
}
