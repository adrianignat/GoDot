using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class Bow : Spawner<Arrow>
{
	private float _timeUntilNextFire = 0f;

	private bool _arrowQueued = false;

	private AnimatedSprite2D _playerAnimation;
	private Player _player;

	public override void _Ready()
	{
		_playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
		_player = GetParent<Player>();
	}

	public override bool CanSpawn()
	{
		if (_player == null || _player.IsDead)
			return false;

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
}
