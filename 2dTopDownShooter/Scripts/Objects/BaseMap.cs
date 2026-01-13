using Godot;
using System.Linq;

public partial class BaseMap : Node2D
{
	private TileSet _worldTileSet;
	private TileSet _decorTileSet;
	private TileSet _noSpawnTileSet;

	public override void _Ready()
	{
		LoadTileSets();
		ApplyTileSets();
	}

	private void LoadTileSets()
	{
		_worldTileSet = GD.Load<TileSet>("res://Resources/WorldTileset_Day.tres");
		_decorTileSet = GD.Load<TileSet>("res://Resources/Decor.tres");
		_noSpawnTileSet = GD.Load<TileSet>("res://Resources/noSpawn.tres");
	}

	private void ApplyTileSets()
	{
		foreach (var layer in GetChildren().OfType<TileMapLayer>())
		{
			string layerName = layer.Name.ToString();

			if (layerName.StartsWith("NoSpawn"))
			{
				layer.TileSet = _noSpawnTileSet;
			}
			else if (layerName.StartsWith("Decor"))
			{
				layer.TileSet = _decorTileSet;
			}
			else
			{
				layer.TileSet = _worldTileSet;
			}
		}
	}
}
