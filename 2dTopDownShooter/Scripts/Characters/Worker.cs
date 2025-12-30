using Godot;

public partial class Worker : CharacterBody2D
{
	[Export] public float Speed = 120f;
	[Export] public float StopDistance = 8f;

	private AnimatedSprite2D _sprite;
	private Vector2 _targetPosition;
	private bool _hasTarget = false;
	private bool _working = false;
	private bool _despawnOnArrival = false;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_hasTarget || _working)
			return;

		Vector2 direction = _targetPosition - GlobalPosition;

		if (direction.Length() <= StopDistance)
		{
			if (_despawnOnArrival)
			{
				Despawn();
				return;
			}
			StartWorking();
			return;
		}

		Velocity = direction.Normalized() * Speed;
		MoveAndSlide();

		if (_sprite.Animation != "run_hammer")
			_sprite.Play("run_hammer");
	}

	public void MoveTo(Vector2 position)
	{
		_targetPosition = position;
		_hasTarget = true;
		_working = false;
		_despawnOnArrival = false;

		_sprite.Play("run_hammer");
	}

	public void MoveToAndDespawn(Vector2 position)
	{
		_targetPosition = position;
		_hasTarget = true;
		_working = false;
		_despawnOnArrival = true;

		_sprite.Play("run_hammer");
	}

	// face the given world position (flip horizontally if target is left)
	public void FaceTowards(Vector2 worldPosition)
	{
		_sprite.FlipH = worldPosition.X < GlobalPosition.X;
	}

	// convenience: always face left
	public void FaceLeft()
	{
		_sprite.FlipH = true;
	}

	private void StartWorking()
	{
		Velocity = Vector2.Zero;
		_working = true;
		_sprite.Play("interact_hammer");
	}

	// 🟢 NEW: idle state
	public void Idle()
	{
		Velocity = Vector2.Zero;
		_hasTarget = false;
		_working = false;

		_sprite.Play("idle");
	}

	// 🔴 disappear
	public void Despawn()
	{
		QueueFree();
	}
}
