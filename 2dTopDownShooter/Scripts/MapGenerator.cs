using Godot;
using System;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts
{
	public partial class MapGenerator : Node2D
	{
		private const int TileSize = 64;
		private const int PieceSize = 1344;    // Each map piece is 1344x1344 pixels
		private const int GridSize = 4;       // 4x4 grid of pieces

		// Map dimensions based on pieces
		private const int MapWidthPixels = PieceSize * GridSize;   // 5376px
		private const int MapHeightPixels = PieceSize * GridSize;  // 5376px

		// For compatibility with existing code
		private const int PlayableWidth = MapWidthPixels / TileSize;  // 84 tiles
		private const int PlayableHeight = MapHeightPixels / TileSize; // 84 tiles
		private const int SpawnMargin = 5;     // tiles on each side for enemy spawning
		private const int MapWidth = PlayableWidth + SpawnMargin * 2;
		private const int MapHeight = PlayableHeight + SpawnMargin * 2;

		// Map piece scene paths
		private const string MapPiecesPath = "res://Entities/MapPieces/";
		private const int MapPieceVariants = 3;  // Each type has 3 variants (_1, _2, _3)

		// Quest buildings
		private const string HouseRuinsPath = "res://Entities/Buildings/house_ruins.tscn";
		private const string TowerRuinsPath = "res://Entities/Buildings/tower_ruins.tscn";
		private const string MonastaryRuinsPath = "res://Entities/Buildings/monastary_ruins.tscn";
		private const string MonasteryPath = "res://Entities/Buildings/monastery.tscn";

		private Random _random;
		private List<Vector2> _housePositions = new();
		private List<StaticBody2D> _houseBodies = new();
		private List<Vector2> _buildingSpawnMarkers = new();
		private HouseRuins _houseRuinsInstance;
		private TowerRuins _towerRuinsInstance;
		private MonastaryRuins _monastaryRuinsInstance;
		private Node2D _monasteryInstance;


	
		public override void _Ready()
		{
			_random = new Random();
			_housePositions = new List<Vector2>();
			_houseBodies = new List<StaticBody2D>();
			_buildingSpawnMarkers = new List<Vector2>();

			GenerateMap();
		}

		public void Regenerate()
		{
			// Clear all existing map children (background, map pieces) but keep the Player
			foreach (Node child in GetChildren())
			{
				if (child is Player)
					continue;
				child.QueueFree();
			}

			// Reset state
			_housePositions.Clear();
			_houseBodies.Clear();
			_buildingSpawnMarkers.Clear();
			_houseRuinsInstance = null;
			_towerRuinsInstance = null;
			_monastaryRuinsInstance = null;
			_monasteryInstance = null;

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
			SpawnHouseRuins();
			SpawnTowerRuins();
			// MonastaryRuins is spawned dynamically by EscortEvent when needed
		}

		private void PlaceMapPieces()
		{
			// Grid layout (4x4):
			// Row 0: top_left,    top,    top,    top_right
			// Row 1: left,       middle, middle, right
			// Row 2: left,       middle, middle, right
			// Row 3: bottom_left, bottom, bottom, bottom_right

			// Place corners (1 random variant each)
			PlaceMapPiece("top_left", 0, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("top_right", 3, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottom_left", 0, 3, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottom_right", 3, 3, _random.Next(1, MapPieceVariants + 1));

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

			// Enable Y-sorting on the piece so its children sort with entities
			piece.YSortEnabled = true;

			// Position based on grid coordinates
			// Add SpawnMargin offset to account for the spawn area around the playable map
			float offsetX = SpawnMargin * TileSize;
			float offsetY = SpawnMargin * TileSize;
			piece.Position = new Vector2(
				gridX * PieceSize + offsetX,
				gridY * PieceSize + offsetY
			);

			AddChild(piece);

			// Track house positions for shelter system
			CollectHousesFromPiece(piece);

			// Collect building spawn markers
			CollectBuildingSpawnMarkers(piece);
		}



		private void SpawnHouseRuins()
		{
			var houseRuinsScene = GD.Load<PackedScene>(HouseRuinsPath);
			if (houseRuinsScene == null)
			{
				GD.PrintErr($"Failed to load house ruins: {HouseRuinsPath}");
				return;
			}

			_houseRuinsInstance = houseRuinsScene.Instantiate<HouseRuins>();

			// TODO: Add designated spawn points on map pieces for quest buildings
			// For now, spawn near player for testing
			Vector2 playerSpawn = GetPlayerSpawnPosition();
			_houseRuinsInstance.Position = playerSpawn + new Vector2(200, 150);
			AddChild(_houseRuinsInstance);
		}

		private void SpawnTowerRuins()
		{
			var towerRuinsScene = GD.Load<PackedScene>(TowerRuinsPath);
			if (towerRuinsScene == null)
			{
				GD.PrintErr($"Failed to load tower ruins: {TowerRuinsPath}");
				return;
			}

			_towerRuinsInstance = towerRuinsScene.Instantiate<TowerRuins>();

			// TODO: Add designated spawn points on map pieces for quest buildings
			// For now, spawn near player for testing
			Vector2 playerSpawn = GetPlayerSpawnPosition();
			_towerRuinsInstance.Position = playerSpawn + new Vector2(-200, 150);
			AddChild(_towerRuinsInstance);
		}

		/// <summary>
		/// Spawn monastary ruins at a specific position. Called by EscortEvent.
		/// </summary>
		public MonastaryRuins SpawnMonastaryRuinsAt(Vector2 position)
		{
			var monastaryRuinsScene = GD.Load<PackedScene>(MonastaryRuinsPath);
			if (monastaryRuinsScene == null)
			{
				GD.PrintErr($"Failed to load monastary ruins: {MonastaryRuinsPath}");
				return null;
			}

			_monastaryRuinsInstance = monastaryRuinsScene.Instantiate<MonastaryRuins>();
			_monastaryRuinsInstance.Position = position;
			AddChild(_monastaryRuinsInstance);

			GD.Print($"[MapGenerator] Spawned monastary ruins at {position}");
			return _monastaryRuinsInstance;
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

		private void CollectBuildingSpawnMarkers(Node2D piece)
		{
			var buildingSpawns = piece.GetNodeOrNull<Node2D>("BuildingSpawns");
			if (buildingSpawns == null)
				return;

			foreach (Node child in buildingSpawns.GetChildren())
			{
				if (child is Marker2D marker)
				{
					Vector2 worldPos = piece.Position + marker.Position;
					_buildingSpawnMarkers.Add(worldPos);
				}
			}
		}

		/// <summary>
		/// Get all building spawn marker positions from map pieces.
		/// </summary>
		public List<Vector2> GetBuildingSpawnMarkers()
		{
			return new List<Vector2>(_buildingSpawnMarkers);
		}

		/// <summary>
		/// Get a random building spawn marker position that is at least minDistance from a given position.
		/// </summary>
		public Vector2? GetSpawnMarkerAwayFrom(Vector2 position, float minDistance)
		{
			var candidates = _buildingSpawnMarkers.FindAll(m => m.DistanceTo(position) >= minDistance);
			if (candidates.Count == 0)
			{
				// Fallback to any marker
				if (_buildingSpawnMarkers.Count > 0)
					return _buildingSpawnMarkers[_random.Next(_buildingSpawnMarkers.Count)];
				return null;
			}
			return candidates[_random.Next(candidates.Count)];
		}

		/// <summary>
		/// Spawn a monastery building at a specific position. Returns the instance.
		/// </summary>
		public Node2D SpawnMonasteryAt(Vector2 position)
		{
			var monasteryScene = GD.Load<PackedScene>(MonasteryPath);
			if (monasteryScene == null)
			{
				GD.PrintErr($"Failed to load monastery: {MonasteryPath}");
				return null;
			}

			_monasteryInstance = monasteryScene.Instantiate<Node2D>();
			_monasteryInstance.Position = position;
			AddChild(_monasteryInstance);

			GD.Print($"[MapGenerator] Spawned monastery at {position}");
			return _monasteryInstance;
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

		public Vector2 GetShelterPosition()
		{
			// If monastary ruins exists and is built, use it as shelter (highest priority)
			if (_monastaryRuinsInstance != null && IsInstanceValid(_monastaryRuinsInstance) && _monastaryRuinsInstance.IsBuilt)
				return _monastaryRuinsInstance.GlobalPosition;

			// If house ruins is built, use it as shelter
			if (_houseRuinsInstance != null && _houseRuinsInstance.IsBuilt)
				return _houseRuinsInstance.GlobalPosition;

			// Otherwise pick a random house
			return GetRandomHousePosition();
		}
	}
}
