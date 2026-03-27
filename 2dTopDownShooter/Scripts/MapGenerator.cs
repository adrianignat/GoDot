using Godot;
using System;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts
{
	public partial class MapGenerator : Node2D
	{
		private const int TileSize = 64;
		private const int PieceSize = 1344;
		private const int GridSize = 4;

		private const int MapWidthPixels = PieceSize * GridSize;
		private const int MapHeightPixels = PieceSize * GridSize;

		private const int PlayableWidth = MapWidthPixels / TileSize;
		private const int PlayableHeight = MapHeightPixels / TileSize;
		private const int SpawnMargin = 5;
		private const int MapWidth = PlayableWidth + SpawnMargin * 2;
		private const int MapHeight = PlayableHeight + SpawnMargin * 2;

		private const string MapPiecesPath = "res://Entities/MapPieces/";
		private const int MapPieceVariants = 3;

		private const string TowerRuinsPath = "res://Entities/Buildings/tower_ruins.tscn";
		private const int TowerRuinsCount = 4;
		private const string MonastaryRuinsPath = "res://Entities/Buildings/monastary_ruins.tscn";
		private const string MonasteryPath = "res://Entities/Buildings/monastery.tscn";
		private const string ArcheryRuinsPath = "res://Entities/Buildings/archery_ruins.tscn";
		private const string GoldmineRuinsPath = "res://Entities/Buildings/goldmine_ruins.tscn";
		private const string SheepScenePath = "res://Entities/Buildings/Sheep.tscn";

		private Random _random;
		private List<Vector2> _housePositions = new();
		private List<StaticBody2D> _houseBodies = new();
		private List<Vector2> _buildingSpawnMarkers = new();
		private List<Vector2> _usedSpawnMarkers = new();
		private List<TowerRuins> _towerRuinsInstances = new();
		private MonastaryRuins _monastaryRuinsInstance;
		private Node2D _monasteryInstance;
		private ArcheryRuins _archeryRuinsInstance;
		private GoldmineRuins _goldmineRuinsInstance;

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
			foreach (Node child in GetChildren())
			{
				if (child is Player)
					continue;
				child.QueueFree();
			}

			_housePositions.Clear();
			_houseBodies.Clear();
			_buildingSpawnMarkers.Clear();
			_usedSpawnMarkers.Clear();
			_towerRuinsInstances.Clear();
			_monastaryRuinsInstance = null;
			_monasteryInstance = null;
			_archeryRuinsInstance = null;
			_goldmineRuinsInstance = null;

			CallDeferred(nameof(GenerateMapDeferred));
		}

		private void GenerateMapDeferred()
		{
			GenerateMap();
		}

		private void GenerateMap()
		{
			PlaceMapPieces();
			SpawnTowerRuins();
		}

		private void PlaceMapPieces()
		{
			PlaceMapPiece("top_left", 0, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("top_right", 3, 0, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottom_left", 0, 3, _random.Next(1, MapPieceVariants + 1));
			PlaceMapPiece("bottom_right", 3, 3, _random.Next(1, MapPieceVariants + 1));

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

			var middleVariants = GetShuffledVariants(4);
			PlaceMapPiece("middle", 1, 1, middleVariants[0]);
			PlaceMapPiece("middle", 2, 1, middleVariants[1]);
			PlaceMapPiece("middle", 1, 2, middleVariants[2]);
			PlaceMapPiece("middle", 2, 2, middleVariants[3]);
		}

		private List<int> GetShuffledVariants(int count)
		{
			var variants = new List<int>();
			while (variants.Count < count)
			{
				for (int i = 1; i <= MapPieceVariants && variants.Count < count; i++)
				{
					variants.Add(i);
				}
			}

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
			piece.YSortEnabled = true;

			float offsetX = SpawnMargin * TileSize;
			float offsetY = SpawnMargin * TileSize;
			piece.Position = new Vector2(
				gridX * PieceSize + offsetX,
				gridY * PieceSize + offsetY
			);

			AddChild(piece);
			CollectHousesFromPiece(piece);
			CollectBuildingSpawnMarkers(piece);
		}

		private void SpawnTowerRuins()
		{
			var towerRuinsScene = GD.Load<PackedScene>(TowerRuinsPath);
			if (towerRuinsScene == null)
			{
				GD.PrintErr($"Failed to load tower ruins: {TowerRuinsPath}");
				return;
			}

			var availableMarkers = new List<Vector2>(_buildingSpawnMarkers);
			int towersToSpawn = Math.Min(TowerRuinsCount, availableMarkers.Count);

			for (int i = 0; i < towersToSpawn; i++)
			{
				int index = _random.Next(availableMarkers.Count);
				Vector2 position = availableMarkers[index];
				availableMarkers.RemoveAt(index);
				_usedSpawnMarkers.Add(position);

				var tower = towerRuinsScene.Instantiate<TowerRuins>();
				tower.Position = position;
				AddChild(tower);
				_towerRuinsInstances.Add(tower);
			}

			GD.Print($"[MapGenerator] Spawned {towersToSpawn} tower ruins at random markers");
		}

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
			_usedSpawnMarkers.Add(position);

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

		public List<Vector2> GetBuildingSpawnMarkers()
		{
			return new List<Vector2>(_buildingSpawnMarkers);
		}

		public List<TowerRuins> GetTowerRuins()
		{
			return _towerRuinsInstances
				.FindAll(ruins => ruins != null && IsInstanceValid(ruins));
		}

		public Vector2? GetSpawnMarkerAwayFrom(Vector2 position, float minDistance)
		{
			var availableMarkers = _buildingSpawnMarkers.FindAll(m => !_usedSpawnMarkers.Contains(m));
			var candidates = availableMarkers.FindAll(m => m.DistanceTo(position) >= minDistance);
			if (candidates.Count == 0)
			{
				if (availableMarkers.Count > 0)
					return availableMarkers[_random.Next(availableMarkers.Count)];
				return null;
			}
			return candidates[_random.Next(candidates.Count)];
		}

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
			_usedSpawnMarkers.Add(position);

			GD.Print($"[MapGenerator] Spawned monastery at {position}");
			return _monasteryInstance;
		}

		public ArcheryRuins SpawnArcheryRuinsAt(Vector2 position)
		{
			var archeryScene = GD.Load<PackedScene>(ArcheryRuinsPath);
			if (archeryScene == null)
			{
				GD.PrintErr($"Failed to load archery ruins: {ArcheryRuinsPath}");
				return null;
			}

			_archeryRuinsInstance = archeryScene.Instantiate<ArcheryRuins>();
			_archeryRuinsInstance.Position = position;
			AddChild(_archeryRuinsInstance);
			_usedSpawnMarkers.Add(position);
			return _archeryRuinsInstance;
		}

		public GoldmineRuins SpawnGoldmineRuinsAt(Vector2 position)
		{
			var goldmineScene = GD.Load<PackedScene>(GoldmineRuinsPath);
			if (goldmineScene == null)
			{
				GD.PrintErr($"Failed to load goldmine ruins: {GoldmineRuinsPath}");
				return null;
			}

			_goldmineRuinsInstance = goldmineScene.Instantiate<GoldmineRuins>();
			_goldmineRuinsInstance.Position = position;
			AddChild(_goldmineRuinsInstance);
			_usedSpawnMarkers.Add(position);
			return _goldmineRuinsInstance;
		}

		public CharacterBody2D SpawnSheepAt(Vector2 position)
		{
			var sheepScene = GD.Load<PackedScene>(SheepScenePath);
			if (sheepScene == null)
			{
				GD.PrintErr($"Failed to load sheep: {SheepScenePath}");
				return null;
			}

			var sheep = sheepScene.Instantiate<CharacterBody2D>();
			sheep.Position = position;
			AddChild(sheep);
			return sheep;
		}

		public Vector2? GetUnusedSpawnMarker()
		{
			var availableMarkers = _buildingSpawnMarkers.FindAll(m => !_usedSpawnMarkers.Contains(m));
			if (availableMarkers.Count == 0)
				return null;

			return availableMarkers[_random.Next(availableMarkers.Count)];
		}


		private bool IsValidEventSpawnPosition(Vector2 position)
		{
			Rect2 playableArea = GetPlayableAreaBounds().Grow(-160f);
			if (!playableArea.HasPoint(position))
				return false;

			Vector2[] samples =
			{
				Vector2.Zero,
				new(120, 0),
				new(-120, 0),
				new(0, 120),
				new(0, -120),
				new(85, 85),
				new(-85, 85),
				new(85, -85),
				new(-85, -85)
			};

			foreach (var offset in samples)
			{
				Vector2 sample = position + offset;
				if (!playableArea.HasPoint(sample) || Game.Instance.IsInNoSpawnZone(sample))
					return false;
			}

			return true;
		}
		public Vector2 GetPlayerSpawnPosition()
		{
			return new Vector2(
				(SpawnMargin + PlayableWidth / 2) * TileSize + TileSize / 2,
				(SpawnMargin + PlayableHeight / 2) * TileSize + TileSize / 2
			);
		}

		public Vector2 GetMapSize()
		{
			return new Vector2(MapWidth * TileSize, MapHeight * TileSize);
		}

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
				return GetPlayerSpawnPosition();

			return _housePositions[_random.Next(_housePositions.Count)];
		}

		public Vector2 GetShelterPosition()
		{
			if (_monastaryRuinsInstance != null && IsInstanceValid(_monastaryRuinsInstance) && _monastaryRuinsInstance.IsBuilt)
				return _monastaryRuinsInstance.GlobalPosition;

			return GetRandomHousePosition();
		}
	}
}
