using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Characters;
using Godot;
using System.Collections.Generic;

public enum EnemyTier
{
	Blue = 1,   // 1 shot to kill (100 health)
	Red = 2,    // 2 shots to kill (200 health)
	Purple = 3, // 3 shots to kill (300 health)
	Yellow = 4  // 4 shots to kill (400 health)
}

/// <summary>
/// Base class for all enemy types. Handles common functionality like
/// tier-based health, sprite loading, movement toward player, and death.
/// </summary>
public abstract partial class Enemy : Character
{
	public EnemyTier Tier { get; set; } = EnemyTier.Blue;

	protected Player _player;
	protected string _animationNodeName = "EnemyAnimations";

	//MISHU - ASTARGRID//
	protected AStarGrid2D _astar;
	protected TileMapLayer _mapLayer;

	protected Queue<Vector2> _path = new();
	protected Vector2 _currentTarget;
	//MISHU - ASTARGRID//





	public override void _Ready()
	{
		
		
		_player = Game.Instance.Player;

		// Let subclass configure settings before we use them
		ConfigureEnemy();

		_animation = GetNode<AnimatedSprite2D>(_animationNodeName);

		// Add to enemies group for cleanup on day transition
		AddToGroup(GameConstants.EnemiesGroup);

		// Initialize health based on tier
		Health = (ushort)(GameConstants.EnemyBaseHealth * (int)Tier);

		// Let subclass load its specific sprites
		LoadSpriteForTier();

		// Connect animation finished for subclass handling
		_animation.AnimationFinished += OnAnimationFinished;

		// Start with walk animation
		_animation.Play("walk");

		// Let subclass do additional initialization
		OnReady();



		
		//MISHU - ASTARGRID//
		var mapNode = GetTree().GetFirstNodeInGroup("map");

		if (mapNode is BaseMap baseMap)
		{
			_astar = baseMap.GetAStar();

			// Find a TileMapLayer to use for map <-> world conversion
			foreach (Node child in baseMap.GetChildren())
			{
				if (child is TileMapLayer layer && layer.TileSet != null)
				{
					_mapLayer = layer;
					break;
				}
			}

			if (_mapLayer == null)
			{
				GD.PushError("[Enemy] No TileMapLayer found on BaseMap for coordinate conversion.");
			}
		}

		
		//MISHU - ASTARGRID//








	}

	/// <summary>
	/// Called before _Ready uses any settings. Override to set _animationNodeName or other config.
	/// </summary>
	protected virtual void ConfigureEnemy() { }

	/// <summary>
	/// Called after base _Ready completes. Override for subclass-specific initialization.
	/// </summary>
	protected virtual void OnReady() { }

	/// <summary>
	/// Load the appropriate sprite for the current tier.
	/// </summary>
	protected abstract void LoadSpriteForTier();

	/// <summary>
	/// Handle animation finished events. Override in subclass for specific behavior.
	/// </summary>
	protected virtual void OnAnimationFinished() { }

	
	
	
	//MISHU - ASTARGRID//
	protected void RecalculatePath()
	{
		GD.Print($"[Enemy] Path length: {_path.Count}");
		if (_astar == null || _mapLayer == null || _player == null)
			return;

		Vector2I startCell = _mapLayer.LocalToMap(GlobalPosition);
		Vector2I endCell = _mapLayer.LocalToMap(_player.GlobalPosition);

		if (!_astar.IsInBoundsv(startCell) || !_astar.IsInBoundsv(endCell))
			return;

		if (_astar.IsPointSolid(startCell) || _astar.IsPointSolid(endCell))
			return;

		_path.Clear();

		var cellPath = _astar.GetIdPath(startCell, endCell);

		if (cellPath.Count == 0)
			return;

		foreach (Vector2I cell in cellPath)
		{
			Vector2 worldPos = _mapLayer.MapToLocal(cell);
			_path.Enqueue(worldPos);
		}

		
	}

	protected Vector2 GetMovementDirection()
	{
		if (_path.Count == 0)
			return Vector2.Zero;

		_currentTarget = _path.Peek();

		if (GlobalPosition.DistanceTo(_currentTarget) < 8f)
			_path.Dequeue();

		return ( _currentTarget - GlobalPosition ).Normalized();
	}
	//MISHU - ASTARGRID//






	internal override void OnKilled()
	{
		Game.Instance.EmitSignal(Game.SignalName.EnemyKilled, this);
		QueueFree();
	}

	/// <summary>
	/// Helper to build animation frames from a sprite sheet.
	/// </summary>
	protected static void BuildAnimationFrames(
		SpriteFrames spriteFrames,
		Texture2D texture,
		(string name, int row, int frameCount, bool loop, float speed)[] animations,
		int frameSize = 192)
	{
		foreach (var (name, row, frameCount, loop, speed) in animations)
		{
			spriteFrames.AddAnimation(name);
			spriteFrames.SetAnimationLoop(name, loop);
			spriteFrames.SetAnimationSpeed(name, speed);

			for (int i = 0; i < frameCount; i++)
			{
				var atlasTexture = new AtlasTexture();
				atlasTexture.Atlas = texture;
				atlasTexture.Region = new Rect2(i * frameSize, row * frameSize, frameSize, frameSize);
				spriteFrames.AddFrame(name, atlasTexture);
			}
		}
	}
}
