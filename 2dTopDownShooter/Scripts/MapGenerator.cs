using Godot;
using System;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts
{
	public partial class MapGenerator : Node2D
	{
		private const int TileSize = 64;
		private const int PlayableWidth = 20;  // tiles (the area where player can move)
		private const int PlayableHeight = 20; // tiles
		private const int SpawnMargin = 5;     // tiles on each side for enemy spawning

		// Total map size includes spawn margins on all sides
		private const int MapWidth = PlayableWidth + SpawnMargin * 2;
		private const int MapHeight = PlayableHeight + SpawnMargin * 2;

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

		// Building scenes
		private static readonly string TowerScene = "res://Entities/Buildings/Tower.tscn";
		private static readonly string HouseScene = "res://Entities/Buildings/House.tscn";
		private static readonly string WoodTowerScene = "res://Entities/Buildings/WoodTower.tscn";
		private static readonly string TreeScene = "res://Entities/Buildings/Tree.tscn";
		private static readonly string SheepScene = "res://Entities/Buildings/Sheep.tscn";

		// Building textures for color variants
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

		// Decor items with spawn weights (higher = more common)
		private static readonly (string path, int weight, int maxCount)[] DecorItems = {
			// Mushrooms - common
			("res://Sprites/Decor/01.png", 10, 0),
			("res://Sprites/Decor/02.png", 10, 0),
			("res://Sprites/Decor/03.png", 10, 0),
			// Rocks - common
			("res://Sprites/Decor/04.png", 10, 0),
			("res://Sprites/Decor/05.png", 10, 0),
			("res://Sprites/Decor/06.png", 10, 0),
			// Bushes - common
			("res://Sprites/Decor/07.png", 8, 0),
			("res://Sprites/Decor/08.png", 8, 0),
			("res://Sprites/Decor/09.png", 8, 0),
			// Small plants - common
			("res://Sprites/Decor/10.png", 8, 0),
			("res://Sprites/Decor/11.png", 8, 0),
			// Pumpkins - moderate
			("res://Sprites/Decor/12.png", 4, 3),
			("res://Sprites/Decor/13.png", 4, 3),
			// Bones - moderate
			("res://Sprites/Decor/14.png", 5, 4),
			("res://Sprites/Decor/15.png", 5, 4),
			// Skeleton sign - rare (max 1)
			("res://Sprites/Decor/16.png", 1, 1),
			// Signpost - rare (max 1)
			("res://Sprites/Decor/17.png", 1, 1),
			// Scarecrow - rare (max 1)
			("res://Sprites/Decor/18.png", 1, 1),
		};

		private Random _random;
		private HashSet<Vector2I> _occupiedTiles;
		private TileMapLayer _groundLayer;
		private TileSet _groundTileSet;
		private List<Vector2> _housePositions = new();
		private List<StaticBody2D> _houseBodies = new();

		public override void _Ready()
		{
			_random = new Random();
			_occupiedTiles = new HashSet<Vector2I>();
			_housePositions = new List<Vector2>();
			_houseBodies = new List<StaticBody2D>();

			GenerateMap();
		}

		public void Regenerate()
		{
			// Clear all existing map children (background, ground tiles, decorations)
			foreach (Node child in GetChildren())
			{
				child.QueueFree();
			}

			// Clear buildings and trees from EntityLayer (they were added there for Y-sorting)
			if (Game.Instance.EntityLayer != null)
			{
				var entityLayer = Game.Instance.EntityLayer;
				foreach (Node child in entityLayer.GetChildren())
				{
					// Remove buildings, trees, and sheep (but not Player)
					if (child.Name.ToString().StartsWith("Fort") ||
						child.Name.ToString().StartsWith("House") ||
						child.Name.ToString().StartsWith("WoodTower") ||
						child.Name.ToString().StartsWith("Tree") ||
						child.Name.ToString().StartsWith("Sheep"))
					{
						child.QueueFree();
					}
				}
			}

			// Reset state
			_occupiedTiles.Clear();
			_housePositions.Clear();
			_houseBodies.Clear();
			_groundLayer = null;
			_groundTileSet = null;

			// Generate new map
			CallDeferred(nameof(GenerateMapDeferred));
		}

		private void GenerateMapDeferred()
		{
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
			// PlaceSheep(); // Disabled for now - too distracting
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
			// Boundary walls surround the playable area, not the full map
			// This allows enemies to spawn in the margin area outside the playable zone
			float wallThickness = 64f;
			float playableX = SpawnMargin * TileSize;
			float playableY = SpawnMargin * TileSize;
			float playableW = PlayableWidth * TileSize;
			float playableH = PlayableHeight * TileSize;

			// Left wall (at left edge of playable area)
			CreateBoundaryWall(new Vector2(playableX - wallThickness / 2, playableY + playableH / 2), new Vector2(wallThickness, playableH + wallThickness * 2));
			// Right wall (at right edge of playable area)
			CreateBoundaryWall(new Vector2(playableX + playableW + wallThickness / 2, playableY + playableH / 2), new Vector2(wallThickness, playableH + wallThickness * 2));
			// Top wall (at top edge of playable area)
			CreateBoundaryWall(new Vector2(playableX + playableW / 2, playableY - wallThickness / 2), new Vector2(playableW + wallThickness * 2, wallThickness));
			// Bottom wall (at bottom edge of playable area)
			CreateBoundaryWall(new Vector2(playableX + playableW / 2, playableY + playableH + wallThickness / 2), new Vector2(playableW + wallThickness * 2, wallThickness));
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
			// Center of the playable area (accounting for spawn margin offset)
			int centerX = SpawnMargin + PlayableWidth / 2;
			int centerY = SpawnMargin + PlayableHeight / 2;
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
			// Place forts near corners of the playable area
			int minX = SpawnMargin + 2;
			int maxX = SpawnMargin + PlayableWidth - 3;
			int minY = SpawnMargin + 2;
			int maxY = SpawnMargin + PlayableHeight - 3;

			var corners = new List<Vector2I>
			{
				new Vector2I(minX, minY),
				new Vector2I(maxX, minY),
				new Vector2I(minX, maxY),
				new Vector2I(maxX, maxY)
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
			var worldPos = TileToWorld(tilePos);
			float baseY = worldPos.Y + 30; // Base of tower visual

			// Instance the tower scene
			var towerScene = GD.Load<PackedScene>(TowerScene);
			var tower = towerScene.Instantiate<Node2D>();
			tower.Name = "Fort";
			tower.Position = new Vector2(worldPos.X, baseY);

			// Randomize texture color
			var sprite = tower.GetNode<Sprite2D>("Sprite");
			sprite.Texture = GD.Load<Texture2D>(TowerTextures[_random.Next(TowerTextures.Length)]);

			Game.Instance.EntityLayer.AddChild(tower);

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
			bool isHouse = textures == HouseTextures;
			string scenePath = isHouse ? HouseScene : WoodTowerScene;
			float baseOffsetY = isHouse ? 35 : 50;

			int placed = 0;
			int attempts = 0;

			while (placed < count && attempts < 100)
			{
				attempts++;

				// Place buildings within the playable area only
				int x = _random.Next(SpawnMargin + 1, SpawnMargin + PlayableWidth - 1);
				int y = _random.Next(SpawnMargin + 1, SpawnMargin + PlayableHeight - 1);
				var tilePos = new Vector2I(x, y);

				if (!_occupiedTiles.Contains(tilePos) && !IsNearOccupied(tilePos, 2))
				{
					var worldPos = TileToWorld(tilePos);
					float baseY = worldPos.Y + baseOffsetY;

					// Instance the building scene
					var buildingScene = GD.Load<PackedScene>(scenePath);
					var building = buildingScene.Instantiate<Node2D>();
					building.Name = isHouse ? "House" : "WoodTower";
					building.Position = new Vector2(worldPos.X, baseY);

					// Randomize texture color
					var sprite = building.GetNode<Sprite2D>("Sprite");
					sprite.Texture = GD.Load<Texture2D>(textures[_random.Next(textures.Length)]);

					// For wood towers, randomize which frame to show (4 variants in spritesheet)
					if (!isHouse)
					{
						sprite.Frame = _random.Next(4);
					}

					// Add to EntityLayer for Y-sorting
					Game.Instance.EntityLayer.AddChild(building);

					// Track house positions for shelter system
					if (isHouse)
					{
						_housePositions.Add(worldPos);
						var body = building.GetNode<StaticBody2D>("CollisionBody");
						_houseBodies.Add(body);
					}

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
			int numDecorations = _random.Next(15, 25);

			// Track spawn counts for items with max limits
			var spawnCounts = new Dictionary<string, int>();

			// Calculate total weight for weighted random selection
			int totalWeight = 0;
			foreach (var item in DecorItems)
			{
				totalWeight += item.weight;
			}

			for (int i = 0; i < numDecorations; i++)
			{
				int attempts = 0;
				while (attempts < 20)
				{
					attempts++;

					// Place decorations within the playable area only
					int x = _random.Next(SpawnMargin, SpawnMargin + PlayableWidth);
					int y = _random.Next(SpawnMargin, SpawnMargin + PlayableHeight);
					var tilePos = new Vector2I(x, y);

					if (!_occupiedTiles.Contains(tilePos))
					{
						// Weighted random selection
						var selectedItem = SelectWeightedDecor(totalWeight, spawnCounts);
						if (selectedItem == null) break; // All items maxed out

						var texture = GD.Load<Texture2D>(selectedItem.Value.path);

						var decor = new Sprite2D();
						decor.Texture = texture;
						var offset = new Vector2(
							_random.Next(-16, 17),
							_random.Next(-16, 17)
						);
						decor.Position = TileToWorld(tilePos) + offset;
						decor.ZIndex = -1;
						AddChild(decor);

						// Track spawn count
						if (!spawnCounts.ContainsKey(selectedItem.Value.path))
							spawnCounts[selectedItem.Value.path] = 0;
						spawnCounts[selectedItem.Value.path]++;

						break;
					}
				}
			}
		}

		private (string path, int weight, int maxCount)? SelectWeightedDecor(int totalWeight, Dictionary<string, int> spawnCounts)
		{
			// Build list of available items (not maxed out)
			var availableItems = new List<(string path, int weight, int maxCount)>();
			int availableWeight = 0;

			foreach (var item in DecorItems)
			{
				// Check if item has reached max count (0 = unlimited)
				if (item.maxCount > 0)
				{
					int currentCount = spawnCounts.GetValueOrDefault(item.path, 0);
					if (currentCount >= item.maxCount)
						continue;
				}
				availableItems.Add(item);
				availableWeight += item.weight;
			}

			if (availableItems.Count == 0 || availableWeight == 0)
				return null;

			// Weighted random selection
			int roll = _random.Next(availableWeight);
			int cumulative = 0;

			foreach (var item in availableItems)
			{
				cumulative += item.weight;
				if (roll < cumulative)
					return item;
			}

			return availableItems[^1]; // Fallback to last item
		}

		private void PlaceTrees()
		{
			int numTrees = _random.Next(8, 15);

			// Playable area bounds for tree placement
			int minX = SpawnMargin;
			int maxX = SpawnMargin + PlayableWidth;
			int minY = SpawnMargin;
			int maxY = SpawnMargin + PlayableHeight;

			var treeSceneResource = GD.Load<PackedScene>(TreeScene);

			for (int i = 0; i < numTrees; i++)
			{
				int attempts = 0;
				while (attempts < 30)
				{
					attempts++;

					int x, y;
					// 70% chance to place trees near edges of playable area
					if (_random.NextDouble() < 0.7)
					{
						if (_random.NextDouble() < 0.5)
						{
							x = _random.NextDouble() < 0.5 ? _random.Next(minX, minX + 3) : _random.Next(maxX - 3, maxX);
							y = _random.Next(minY, maxY);
						}
						else
						{
							x = _random.Next(minX, maxX);
							y = _random.NextDouble() < 0.5 ? _random.Next(minY, minY + 3) : _random.Next(maxY - 3, maxY);
						}
					}
					else
					{
						x = _random.Next(minX, maxX);
						y = _random.Next(minY, maxY);
					}

					var tilePos = new Vector2I(x, y);

					if (!_occupiedTiles.Contains(tilePos) && !IsNearOccupied(tilePos, 1))
					{
						var worldPos = TileToWorld(tilePos);

						// Trunk collision is at Y+40 from center
						float trunkOffsetY = 40;
						float baseY = worldPos.Y + trunkOffsetY;

						// Instance the tree scene
						var tree = treeSceneResource.Instantiate<Node2D>();
						tree.Name = "Tree";
						tree.Position = new Vector2(worldPos.X, baseY);

						// Randomize tree variant (frames 0-5 are full trees)
						var sprite = tree.GetNode<Sprite2D>("Sprite");
						sprite.Frame = _random.Next(0, 6);

						// Add to EntityLayer for Y-sorting
						Game.Instance.EntityLayer.AddChild(tree);

						_occupiedTiles.Add(tilePos);
						break;
					}
				}
			}
		}

		private void PlaceSheep()
		{
			int numSheep = _random.Next(2, 5);

			// Playable area bounds
			int minX = SpawnMargin + 2;
			int maxX = SpawnMargin + PlayableWidth - 2;
			int minY = SpawnMargin + 2;
			int maxY = SpawnMargin + PlayableHeight - 2;

			var sheepSceneResource = GD.Load<PackedScene>(SheepScene);

			for (int i = 0; i < numSheep; i++)
			{
				int attempts = 0;
				while (attempts < 30)
				{
					attempts++;

					int x = _random.Next(minX, maxX);
					int y = _random.Next(minY, maxY);
					var tilePos = new Vector2I(x, y);

					if (!_occupiedTiles.Contains(tilePos) && !IsNearOccupied(tilePos, 1))
					{
						var worldPos = TileToWorld(tilePos);

						// Instance the sheep scene
						var sheep = sheepSceneResource.Instantiate<Node2D>();
						sheep.Name = "Sheep";
						sheep.Position = worldPos;

						// Randomize which sheep frame to show (8 variants)
						var sprite = sheep.GetNode<Sprite2D>("Sprite");
						sprite.Frame = _random.Next(0, 8);

						// Random facing direction
						sprite.FlipH = _random.Next(2) == 0;

						// Add to EntityLayer for Y-sorting
						Game.Instance.EntityLayer.AddChild(sheep);

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
			// Spawn in center of the playable area
			return new Vector2(
				(SpawnMargin + PlayableWidth / 2) * TileSize + TileSize / 2,
				(SpawnMargin + PlayableHeight / 2) * TileSize + TileSize / 2
			);
		}

		public Vector2 GetMapSize()
		{
			return new Vector2(MapWidth * TileSize, MapHeight * TileSize);
		}

		/// <summary>
		/// Returns the bounds of the playable area for camera limits.
		/// Returns (x, y, width, height) where x,y is the top-left corner.
		/// </summary>
		public Rect2 GetPlayableAreaBounds()
		{
			return new Rect2(
				SpawnMargin * TileSize,
				SpawnMargin * TileSize,
				PlayableWidth * TileSize,
				PlayableHeight * TileSize
			);
		}

		public List<Vector2> GetHousePositions()
		{
			return _housePositions;
		}

		public Vector2 GetRandomHousePosition()
		{
			if (_housePositions.Count == 0)
				return GetPlayerSpawnPosition(); // Fallback to center

			return _housePositions[_random.Next(_housePositions.Count)];
		}
	}
}
