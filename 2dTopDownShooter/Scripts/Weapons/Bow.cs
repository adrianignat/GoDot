using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Spawners;
using dTopDownShooter.Scripts.Upgrades;
using Godot;

public partial class Bow : Spawner<Arrow>
{
	private float _timeUntilNextFire = 0f;

	private bool _arrowQueued = false;

	private AnimatedSprite2D _playerAnimation;
	private Player _player;
	private Camera2D _camera;

	/// <summary>
	/// Number of additional enemies arrows can bounce through.
	/// </summary>
	public int BouncingLevel { get; set; } = 0;

	/// <summary>
	/// Number of additional enemies arrows can pierce through.
	/// </summary>
	public int PiercingLevel { get; set; } = 0;

	/// <summary>
	/// Tracks which arrow upgrade type was selected (Bouncing or Piercing).
	/// Used to make them mutually exclusive.
	/// </summary>
	public UpgradeType? SelectedArrowUpgrade { get; set; } = null;

	public override void _Ready()
	{
		_playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
		_player = GetParent<Player>();
		_camera = _player.GetNode<Camera2D>("Camera2D");
	}

	public override bool CanSpawn()
	{
		// Don't shoot if no enemies are visible
		if (!HasVisibleEnemy())
		{
			_arrowQueued = false;
			return false;
		}

		if (!_player.IsShooting && !_arrowQueued)
		{
			_arrowQueued = true;
			_player.PlayShootAnimation();
			return false;
		} else if (!_player.IsShooting && _arrowQueued)
		{
			_arrowQueued = false;
			return true;
		}
		return false;
	}

	private bool HasVisibleEnemy()
	{
		var viewRect = GetVisibleRect();
		var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);

		foreach (Node2D enemy in enemies)
		{
			if (viewRect.HasPoint(enemy.GlobalPosition))
				return true;
		}
		return false;
	}

	public Rect2 GetVisibleRect()
	{
		Vector2 center = _camera.GetScreenCenterPosition();
		Vector2 halfSize = GetViewportRect().Size / _camera.Zoom / 2;
		return new Rect2(center - halfSize, halfSize * 2);
	}

	protected override void InitializeSpawnedObject(Arrow arrow)
	{
		arrow.BouncingLevel = BouncingLevel;
		arrow.PiercingLevel = PiercingLevel;
	}
}
