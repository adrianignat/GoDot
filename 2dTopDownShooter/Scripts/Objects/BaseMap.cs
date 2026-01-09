using Godot;
using System.Linq;
using System.Collections.Generic;

public partial class BaseMap : Node2D
{
    private TileSet _worldTileSet;
    private TileSet _decorTileSet;
    private TileSet _noSpawnTileSet;
    private AStarGrid2D _astar;
    private const int TILE_SIZE = 64;

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

    private void BuildAStarGrid()
    {
        _astar = new AStarGrid2D();

        // Assume all TileMapLayers share the same used rect
        TileMapLayer referenceLayer = GetChildren()
            .OfType<TileMapLayer>()
            .FirstOrDefault(l => l.TileSet == _worldTileSet);

        if (referenceLayer == null)
        {
            GD.PushError("[BaseMap] No WorldTileset layer found for AStarGrid2D.");
            return;
        }

        Rect2I usedRect = referenceLayer.GetUsedRect();

        _astar.Region = usedRect;
        _astar.CellSize = new Vector2(TILE_SIZE, TILE_SIZE);

        _astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
        _astar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
        _astar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan;

        _astar.Update();

        // Scan all layers that use the world tileset
        foreach (var layer in GetChildren().OfType<TileMapLayer>())
        {
            if (layer.TileSet != _worldTileSet)
                continue;

            foreach (Vector2I cell in layer.GetUsedCells())
            {
                TileData tileData = layer.GetCellTileData(cell);
                if (tileData == null)
                    continue;

                // Read custom data: "obstacle"
                Variant obstacleVar = tileData.GetCustomData("obstacle");

                bool isObstacle =
                    obstacleVar.VariantType == Variant.Type.Bool &&
                    obstacleVar.AsBool();


                if (isObstacle)
                {
                    _astar.SetPointSolid(cell, true);
                }
            }
        }
    }

    public AStarGrid2D GetAStar()
    {
        return _astar;  
    }

}
