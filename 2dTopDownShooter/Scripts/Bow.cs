using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class Bow : Spawner<Arrow>
{
	private float _timeUntilNextFire = 0f;

	private bool _arrowQueued = false;

	private AnimatedSprite2D _playerAnimation;
	private Player _player;

	/// <summary>
	/// Number of additional enemies arrows can bounce through.
	/// </summary>
	public int BouncingLevel { get; set; } = 0;

	public override void _Ready()
	{
		_playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
		_player = GetParent<Player>();
	}

	public override bool CanSpawn()
	{
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

	protected override void InitializeSpawnedObject(Arrow arrow)
	{
		arrow.BouncingLevel = BouncingLevel;
	}
}
