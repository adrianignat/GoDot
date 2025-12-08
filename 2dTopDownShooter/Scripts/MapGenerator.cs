using Godot;
using System;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts
{
	public partial class MapGenerator : Node2D
	{
		private const int TileSize = 64;
		private const int PieceSize = 640;    // Each map piece is 640x640 pixels
		private const int GridSize = 4;       // 4x4 grid of pieces

		// Map dimensions based on pieces
		private const int MapWidthPixels = PieceSize * GridSize;   // 2560px
		private const int MapHeightPixels = PieceSize * GridSize;  // 2560px

		// For compatibility with existing code
		private const int PlayableWidth = MapWidthPixels / TileSize;  // 40 tiles
		private const int PlayableHeight = MapHeightPixels / TileSize; // 40 tiles
		private const int SpawnMargin = 5;     // tiles on each side for enemy spawning
		private const int MapWidth = PlayableWidth + SpawnMargin * 2;
		private const int MapHeight = PlayableHeight + SpawnMargin * 2;

		// Map piece scene paths
		private const string MapPiecesPath = "res://Entities/MapPieces/";
		private const int MapPieceVariants = 3;  // Each type has 3 variants (_1, _2, _3)

		private Random _random;
		private List<Vector2> _housePositions = new();
		private List<StaticBody2D> _houseBodies = new();

		public override void _Ready()
		{
			_random = new Random();
			_housePositions = new List<Vector2>();
			_houseBodies = new List<StaticBody2D>();

			GenerateMap();
		}

		public void Regenerate()
		{
			// Clear all existing map children (background, map pieces)
			foreach (Node child in GetChildren())
			{
				child.QueueFree();
			}

			// Reset state
			_housePositions.Clear();
			_houseBodies.Clear();

			// Generate new map
			CallDeferred(nameof(GenerateMapDeferred));
		}

		private void GenerateMapDeferred()
		{
			GenerateMap();
		}

		private void GenerateMap()
		{
			PlaceMapPieces();
			CreateMapBoundaries();
		}

		private void PlaceMapPieces()
		{
			// Grid layout (4x4):
			// Row 0: topleft,    top,    top,    topright
			// Row 1: left,       middle, middle, right
			// Row 2: left,       middle, middle, right
			// Row 3: bottomleft, bottom, bottom, bottomright

			// Place corners (1 random variant each)
			PlaceMapPiece("topleft", 0, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("topright", 3, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottomleft", 0, 3, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottomright", 3, 3, _random.Next(1, MapPieceVariants + 1));

			// Place edge pieces (2 each, different variants)
			var topVariants = GetShuffledVariants(2);
			PlaceMapPiece("top", 1, 0, topVariants[0]);
			PlaceMapPiece("top", 2, 0, topVariants[1]);

			var bottomVariants = GetShuffledVariants(2);
			PlaceMapPiece("bottom", 1, 3, bottomVariants[0]);
			PlaceMapPiece("bottom", 2, 3, bottomVariants[1]);

			var leftVariants = GetShuffledVariants(2);
			PlaceMapPiece("left", 0, 1, leftVariants[0]);
			PlaceMapPiece("left", 0, 2, leftVariants[1]);

			var rightVariants = GetShuffledVariants(2);
			PlaceMapPiece("right", 3, 1, rightVariants[0]);
			PlaceMapPiece("right", 3, 2, rightVariants[1]);

			// Place middle pieces (4 pieces, use all 3 variants + 1 random repeat)
			var middleVariants = GetShuffledVariants(4);
			PlaceMapPiece("middle", 1, 1, middleVariants[0]);
			PlaceMapPiece("middle", 2, 1, middleVariants[1]);
			PlaceMapPiece("middle", 1, 2, middleVariants[2]);
			PlaceMapPiece("middle", 2, 2, middleVariants[3]);
		}

		private List<int> GetShuffledVariants(int count)
		{
			// Create list with all variants, repeat if needed
			var variants = new List<int>();
			while (variants.Count < count)
			{
				for (int i = 1; i <= MapPieceVariants && variants.Count < count; i++)
				{
					variants.Add(i);
				}
			}

			// Shuffle
			for (int i = variants.Count - 1; i > 0; i--)
			{
				int j = _random.Next(i + 1);
				(variants[i], variants[j]) = (variants[j], variants[i]);
			}

			return variants;
		}

		private void PlaceMapPiece(string pieceType, int gridX, int gridY, int variant)
		{
			string scenePath = $"{MapPiecesPath}map_piece_{pieceType}_{variant}.tscn";

			var pieceScene = GD.Load<PackedScene>(scenePath);
			if (pieceScene == null)
			{
				GD.PrintErr($"Failed to load map piece: {scenePath}");
				return;
			}

			var piece = pieceScene.Instantiate<Node2D>();

			// Position based on grid coordinates
			// Add SpawnMargin offset to account for the spawn area around the playable map
			float offsetX = SpawnMargin * TileSize;
			float offsetY = SpawnMargin * TileSize;
			piece.Position = new Vector2(
				gridX * PieceSize + offsetX,
				gridY * PieceSize + offsetY
			);

			piece.ZIndex = -10;
			AddChild(piece);

			// Track house positions for shelter system
			CollectHousesFromPiece(piece);
		}

		private void CollectHousesFromPiece(Node2D piece)
		{
			foreach (Node child in piece.GetChildren())
			{
				if (child.Name.ToString().StartsWith("House") && child is Node2D houseNode)
				{
					Vector2 worldPos = piece.Position + houseNode.Position;
					_housePositions.Add(worldPos);

					var body = houseNode.GetNodeOrNull<StaticBody2D>("CollisionBody");
					if (body != null)
					{
						_houseBodies.Add(body);
					}
				}
			}
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
