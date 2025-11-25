using Godot;
using System;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts
{
	public partial class MapGenerator : Node2D
	{
		private const int TileSize = 64;
		private const int MapWidth = 20;  // tiles
		private const int MapHeight = 20; // tiles

		// Tile coordinates based on 2x2 block pattern in Tilemap_Flat.png
		// The tileset uses 2x2 blocks that tile seamlessly when repeated
		private static class GrassTiles
		{
			// Interior tile with no edges (adjust tile coordinates if needed)
			public static readonly Vector2I Interior = new(1, 1);

			// Edge tiles from the 2x2 grass block at (0,0)-(1,1)
			public static readonly Vector2I TopLeft = new(0, 0);
			public static readonly Vector2I TopRight = new(7, 0);
			public static readonly Vector2I BottomLeft = new(0, 7);
			public static readonly Vector2I BottomRight = new(2, 7);
		}

		private static class SandTiles
		{
			// The 2x2 sand block at (5,0)-(6,1)
			public static readonly Vector2I TopLeft = new(5, 0);
			public static readonly Vector2I TopRight = new(7, 0);
			public static readonly Vector2I BottomLeft = new(5, 1);
			public static readonly Vector2I BottomRight = new(7, 1);
		}

		// Building textures
		private static readonly string[] TowerTextures = {
			"res://Sprites/Buildings/Tower/Tower_Blue.png",
			"res://Sprites/Buildings/Tower/Tower_Red.png",
			"res://Sprites/Buildings/Tower/Tower_Purple.png",
			"res://Sprites/Buildings/Tower/Tower_Yellow.png",
		};

		private static readonly string[] HouseTextures = {
			"res://Sprites/Buildings/House/House_Blue.png",
			"res://Sprites/Buildings/House/House_Red.png",
			"res://Sprites/Buildings/House/House_Purple.png",
			"res://Sprites/Buildings/House/House_Yellow.png",
		};

		private static readonly string[] WoodTowerTextures = {
			"res://Sprites/Buildings/Wood_Tower/Wood_Tower_Blue.png",
			"res://Sprites/Buildings/Wood_Tower/Wood_Tower_Red.png",
			"res://Sprites/Buildings/Wood_Tower/Wood_Tower_Purple.png",
			"res://Sprites/Buildings/Wood_Tower/Wood_Tower_Yellow.png",
		};

		private static readonly string[] DecorTextures = {
			"res://Sprites/Decor/01.png",
			"res://Sprites/Decor/02.png",
			"res://Sprites/Decor/03.png",
			"res://Sprites/Decor/04.png",
			"res://Sprites/Decor/05.png",
			"res://Sprites/Decor/06.png",
		};

		private Random _random;
		private HashSet<Vector2I> _occupiedTiles;
		private TileMapLayer _groundLayer;
		private TileSet _groundTileSet;

		public override void _Ready()
		{
			_random = new Random();
			_occupiedTiles = new HashSet<Vector2I>();

			GenerateMap();
		}

		private void GenerateMap()
		{
			CreateBackground();
			CreateGroundTileSet();
			CreateGroundLayer();
			PlaceGroundTiles();
			PlaceStructures();
			PlaceDecorations();
			PlaceTrees();
			CreateMapBoundaries();
		}

		private void CreateBackground()
		{
			var background = new ColorRect();
			background.Color = new Color(0.486f, 0.678f, 0.337f);
			background.Size = new Vector2(MapWidth * TileSize, MapHeight * TileSize);
			background.ZIndex = -20;
			AddChild(background);
		}

		private void CreateMapBoundaries()
		{
			float wallThickness = 64f;
			float mapW = MapWidth * TileSize;
			float mapH = MapHeight * TileSize;

			CreateBoundaryWall(new Vector2(-wallThickness / 2, mapH / 2), new Vector2(wallThickness, mapH + wallThickness * 2));
			CreateBoundaryWall(new Vector2(mapW + wallThickness / 2, mapH / 2), new Vector2(wallThickness, mapH + wallThickness * 2));
			CreateBoundaryWall(new Vector2(mapW / 2, -wallThickness / 2), new Vector2(mapW + wallThickness * 2, wallThickness));
			CreateBoundaryWall(new Vector2(mapW / 2, mapH + wallThickness / 2), new Vector2(mapW + wallThickness * 2, wallThickness));
		}

		private void CreateBoundaryWall(Vector2 position, Vector2 size)
		{
			var wall = new StaticBody2D();
			wall.Position = position;
			wall.CollisionLayer = 4;
			wall.CollisionMask = 0;

			var collision = new CollisionShape2D();
			var shape = new RectangleShape2D();
			shape.Size = size;
			collision.Shape = shape;
			wall.AddChild(collision);
			AddChild(wall);
		}

		private void CreateGroundTileSet()
		{
			_groundTileSet = new TileSet();
			_groundTileSet.TileSize = new Vector2I(TileSize, TileSize);

			var tilemapTexture = GD.Load<Texture2D>("res://Sprites/Tilemap_Flat.png");

			var atlasSource = new TileSetAtlasSource();
			atlasSource.Texture = tilemapTexture;
			atlasSource.TextureRegionSize = new Vector2I(TileSize, TileSize);
			atlasSource.UseTexturePadding = true;

			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 4; y++)
				{
					atlasSource.CreateTile(new Vector2I(x, y));
				}
			}

			_groundTileSet.AddSource(atlasSource);
		}

		private void CreateGroundLayer()
		{
			_groundLayer = new TileMapLayer();
			_groundLayer.Name = "Ground";
			_groundLayer.TileSet = _groundTileSet;
			_groundLayer.ZIndex = -10;
			_groundLayer.AddToGroup("validSpawnLocation");
			AddChild(_groundLayer);
		}

		private void PlaceGroundTiles()
		{
			// Step 1: Fill entire map with interior grass tiles
			for (int x = 0; x < MapWidth; x++)
			{
				for (int y = 0; y < MapHeight; y++)
				{
					_groundLayer.SetCell(new Vector2I(x, y), 0, GrassTiles.Interior);
				}
			}

			//// Step 2: Set edge tiles
			//// Top edge
			//for (int x = 0; x < MapWidth; x++)
			//{
			//	_groundLayer.SetCell(new Vector2I(x, 0), 0, (x % 2 == 0) ? GrassTiles.TopLeft : GrassTiles.TopRight);
			//}
			//// Bottom edge
			//for (int x = 0; x < MapWidth; x++)
			//{
			//	_groundLayer.SetCell(new Vector2I(x, MapHeight - 1), 0, (x % 2 == 0) ? GrassTiles.BottomLeft : GrassTiles.BottomRight);
			//}
			//// Left edge
			//for (int y = 0; y < MapHeight; y++)
			//{
			//	_groundLayer.SetCell(new Vector2I(0, y), 0, (y % 2 == 0) ? GrassTiles.TopLeft : GrassTiles.BottomLeft);
			//}
			//// Right edge
			//for (int y = 0; y < MapHeight; y++)
			//{
			//	_groundLayer.SetCell(new Vector2I(MapWidth - 1, y), 0, (y % 2 == 0) ? GrassTiles.TopRight : GrassTiles.BottomRight);
			//}

			//// Step 3: Set corners (override edges)
			//_groundLayer.SetCell(new Vector2I(0, 0), 0, GrassTiles.TopLeft);
			//_groundLayer.SetCell(new Vector2I(MapWidth - 1, 0), 0, GrassTiles.TopRight);
			//_groundLayer.SetCell(new Vector2I(0, MapHeight - 1), 0, GrassTiles.BottomLeft);
			//_groundLayer.SetCell(new Vector2I(MapWidth - 1, MapHeight - 1), 0, GrassTiles.BottomRight);

			// Add random sand patches on top with their own edges
			//int numSandAreas = _random.Next(2, 4);
			//for (int i = 0; i < numSandAreas; i++)
			//{
			//	CreateSandPatch();
			//}
		}

		private void FillRectWithTerrain(int startX, int startY, int width, int height, bool isGrass)
		{
			for (int x = startX; x < startX + width; x++)
			{
				for (int y = startY; y < startY + height; y++)
				{
					Vector2I tile = GetTileForPosition(x, y, startX, startY, width, height, isGrass);
					_groundLayer.SetCell(new Vector2I(x, y), 0, tile);
				}
			}
		}

		private Vector2I GetTileForPosition(int x, int y, int startX, int startY, int width, int height, bool isGrass)
		{
			bool isTopEdge = (y == startY);
			bool isBottomEdge = (y == startY + height - 1);
			bool isLeftEdge = (x == startX);
			bool isRightEdge = (x == startX + width - 1);

			// The 2x2 block tiles seamlessly when we use:
			// - Tiles with TOP edge on the top row of the area
			// - Tiles with BOTTOM edge on the bottom row
			// - Tiles with LEFT edge on the left column
			// - Tiles with RIGHT edge on the right column
			// For interior, alternate based on position to create seamless pattern

			// Determine which row of the 2x2 block to use (0=top row, 1=bottom row)
			int blockRow;
			if (isTopEdge)
				blockRow = 0; // Use tiles with top edge
			else if (isBottomEdge)
				blockRow = 1; // Use tiles with bottom edge
			else
				blockRow = (y - startY) % 2; // Alternate for seamless interior

			// Determine which column of the 2x2 block to use (0=left col, 1=right col)
			int blockCol;
			if (isLeftEdge)
				blockCol = 0; // Use tiles with left edge
			else if (isRightEdge)
				blockCol = 1; // Use tiles with right edge
			else
				blockCol = (x - startX) % 2; // Alternate for seamless interior

			// Return the appropriate tile from the 2x2 block
			if (isGrass)
			{
				if (blockRow == 0 && blockCol == 0) return GrassTiles.TopLeft;
				if (blockRow == 0 && blockCol == 1) return GrassTiles.TopRight;
				if (blockRow == 1 && blockCol == 0) return GrassTiles.BottomLeft;
				return GrassTiles.BottomRight;
			}
			else
			{
				if (blockRow == 0 && blockCol == 0) return SandTiles.TopLeft;
				if (blockRow == 0 && blockCol == 1) return SandTiles.TopRight;
				if (blockRow == 1 && blockCol == 0) return SandTiles.BottomLeft;
				return SandTiles.BottomRight;
			}
		}

		private void CreateSandPatch()
		{
			// Create a sand patch with minimum size 3x3 to show all 9-slice parts
			int width = _random.Next(3, 6);
			int height = _random.Next(3, 6);
			int startX = _random.Next(1, MapWidth - width - 1);
			int startY = _random.Next(1, MapHeight - height - 1);

			FillRectWithTerrain(startX, startY, width, height, isGrass: false);
		}

		private void PlaceStructures()
		{
			int centerX = MapWidth / 2;
			int centerY = MapHeight / 2;
			for (int x = centerX - 2; x <= centerX + 2; x++)
			{
				for (int y = centerY - 2; y <= centerY + 2; y++)
				{
					_occupiedTiles.Add(new Vector2I(x, y));
				}
			}

			int numForts = _random.Next(1, 3);
			PlaceForts(numForts);

			int numHouses = _random.Next(2, 5);
			PlaceBuildings(numHouses, HouseTextures, new Vector2(64, 96));

			int numWoodTowers = _random.Next(1, 4);
			PlaceBuildings(numWoodTowers, WoodTowerTextures, new Vector2(64, 128));
		}

		private void PlaceForts(int count)
		{
			var corners = new List<Vector2I>
			{
				new Vector2I(2, 2),
				new Vector2I(MapWidth - 3, 2),
				new Vector2I(2, MapHeight - 3),
				new Vector2I(MapWidth - 3, MapHeight - 3)
			};

			for (int i = corners.Count - 1; i > 0; i--)
			{
				int j = _random.Next(i + 1);
				(corners[i], corners[j]) = (corners[j], corners[i]);
			}

			for (int i = 0; i < Math.Min(count, corners.Count); i++)
			{
				var pos = corners[i];
				if (!_occupiedTiles.Contains(pos))
				{
					CreateFort(pos);
				}
			}
		}

		private void CreateFort(Vector2I tilePos)
		{
			var towerTexture = GD.Load<Texture2D>(TowerTextures[_random.Next(TowerTextures.Length)]);

			var tower = new Sprite2D();
			tower.Texture = towerTexture;
			tower.Position = TileToWorld(tilePos);
			tower.ZIndex = 0;
			AddChild(tower);

			var towerBody = new StaticBody2D();
			towerBody.Position = TileToWorld(tilePos);
			towerBody.CollisionLayer = 1;
			towerBody.CollisionMask = 0;

			var towerCollision = new CollisionShape2D();
			var towerShape = new RectangleShape2D();
			towerShape.Size = new Vector2(48, 48);
			towerCollision.Shape = towerShape;
			towerCollision.Position = new Vector2(0, 20);
			towerBody.AddChild(towerCollision);
			AddChild(towerBody);

			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					_occupiedTiles.Add(new Vector2I(tilePos.X + dx, tilePos.Y + dy));
				}
			}
		}

		private void PlaceBuildings(int count, string[] textures, Vector2 collisionSize)
		{
			int placed = 0;
			int attempts = 0;

			while (placed < count && attempts < 100)
			{
				attempts++;

				int x = _random.Next(1, MapWidth - 1);
				int y = _random.Next(1, MapHeight - 1);
				var tilePos = new Vector2I(x, y);

				if (!_occupiedTiles.Contains(tilePos) && !IsNearOccupied(tilePos, 2))
				{
					var texture = GD.Load<Texture2D>(textures[_random.Next(textures.Length)]);

					var building = new Sprite2D();
					building.Texture = texture;
					building.Position = TileToWorld(tilePos);
					building.ZIndex = 0;
					AddChild(building);

					var body = new StaticBody2D();
					body.Position = TileToWorld(tilePos);
					body.CollisionLayer = 1;
					body.CollisionMask = 0;

					var collision = new CollisionShape2D();
					var shape = new RectangleShape2D();
					shape.Size = new Vector2(collisionSize.X * 0.6f, collisionSize.Y * 0.3f);
					collision.Shape = shape;
					collision.Position = new Vector2(0, collisionSize.Y * 0.3f);
					body.AddChild(collision);
					AddChild(body);

					for (int dx = -1; dx <= 1; dx++)
					{
						for (int dy = -1; dy <= 1; dy++)
						{
							_occupiedTiles.Add(new Vector2I(x + dx, y + dy));
						}
					}

					placed++;
				}
			}
		}

		private void PlaceDecorations()
		{
			int numDecorations = _random.Next(10, 20);

			for (int i = 0; i < numDecorations; i++)
			{
				int attempts = 0;
				while (attempts < 20)
				{
					attempts++;

					int x = _random.Next(0, MapWidth);
					int y = _random.Next(0, MapHeight);
					var tilePos = new Vector2I(x, y);

					if (!_occupiedTiles.Contains(tilePos))
					{
						var texture = GD.Load<Texture2D>(DecorTextures[_random.Next(DecorTextures.Length)]);

						var decor = new Sprite2D();
						decor.Texture = texture;
						var offset = new Vector2(
							_random.Next(-16, 17),
							_random.Next(-16, 17)
						);
						decor.Position = TileToWorld(tilePos) + offset;
						decor.ZIndex = -1;
						AddChild(decor);

						break;
					}
				}
			}
		}

		private void PlaceTrees()
		{
			var treeTexture = GD.Load<Texture2D>("res://Sprites/Decor/Trees/Tree.png");

			int numTrees = _random.Next(8, 15);

			for (int i = 0; i < numTrees; i++)
			{
				int attempts = 0;
				while (attempts < 30)
				{
					attempts++;

					int x, y;
					if (_random.NextDouble() < 0.7)
					{
						if (_random.NextDouble() < 0.5)
						{
							x = _random.NextDouble() < 0.5 ? _random.Next(0, 3) : _random.Next(MapWidth - 3, MapWidth);
							y = _random.Next(0, MapHeight);
						}
						else
						{
							x = _random.Next(0, MapWidth);
							y = _random.NextDouble() < 0.5 ? _random.Next(0, 3) : _random.Next(MapHeight - 3, MapHeight);
						}
					}
					else
					{
						x = _random.Next(0, MapWidth);
						y = _random.Next(0, MapHeight);
					}

					var tilePos = new Vector2I(x, y);

					if (!_occupiedTiles.Contains(tilePos) && !IsNearOccupied(tilePos, 1))
					{
						var tree = new Sprite2D();
						tree.Texture = treeTexture;
						tree.Hframes = 4;
						tree.Vframes = 3;
						tree.Frame = _random.Next(0, 6);
						tree.Position = TileToWorld(tilePos);
						tree.ZIndex = 1;
						AddChild(tree);

						var body = new StaticBody2D();
						body.Position = TileToWorld(tilePos);
						body.CollisionLayer = 1;
						body.CollisionMask = 0;

						var collision = new CollisionShape2D();
						var shape = new CircleShape2D();
						shape.Radius = 16;
						collision.Shape = shape;
						collision.Position = new Vector2(0, 40);
						body.AddChild(collision);
						AddChild(body);

						_occupiedTiles.Add(tilePos);
						break;
					}
				}
			}
		}

		private bool IsNearOccupied(Vector2I pos, int radius)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					if (_occupiedTiles.Contains(new Vector2I(pos.X + dx, pos.Y + dy)))
					{
						return true;
					}
				}
			}
			return false;
		}

		private Vector2 TileToWorld(Vector2I tilePos)
		{
			return new Vector2(
				tilePos.X * TileSize + TileSize / 2,
				tilePos.Y * TileSize + TileSize / 2
			);
		}

		public Vector2 GetPlayerSpawnPosition()
		{
			return new Vector2(
				(MapWidth / 2) * TileSize + TileSize / 2,
				(MapHeight / 2) * TileSize + TileSize / 2
			);
		}

		public Vector2 GetMapSize()
		{
			return new Vector2(MapWidth * TileSize, MapHeight * TileSize);
		}
	}
}
