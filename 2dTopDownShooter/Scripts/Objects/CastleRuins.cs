using dTopDownShooter.Scripts;
using Godot;

public partial class CastleRuins : Node2D
{
	[Export] public Texture2D DestroyedTexture;
	[Export] public Texture2D ConstructionTexture;
	[Export] public Texture2D BuiltTexture;

	[Export] public PackedScene WorkerScene;
	[Export] public float BuildTimeRequired = 5f;

	private Sprite2D _sprite;
	private Timer _buildTimer;
	private Marker2D _workerSpawnPoint;
	private Marker2D _workerWorkPoint;

	private Worker _workerInstance;
	private float _buildProgress = 0f;
	private bool _playerInRange = false;

	private enum CastleState
	{
		Destroyed,
		Building,
		Built
	}
	private CastleState _state = CastleState.Destroyed;

	public bool IsBuilt => _state == CastleState.Built;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_buildTimer = GetNode<Timer>("Timer");
		_workerSpawnPoint = GetNode<Marker2D>("WorkerSpawnPoint");
		_workerWorkPoint = GetNode<Marker2D>("WorkerWorkPoint");

		_buildTimer.Timeout += OnBuildTick;
		_buildTimer.WaitTime = 1.0; // 1 second per tick

		_sprite.Texture = DestroyedTexture;

		// fallback: load worker scene if not assigned in the inspector
		if (WorkerScene == null)
			WorkerScene = GD.Load<PackedScene>("res://Entities/Characters/worker.tscn");

	}

	public void OnPlayerDetectionEntered(Node2D body)
	{
		if (!body.IsInGroup(GameConstants.PlayerGroup) || _state == CastleState.Built)
			return;

		OnPlayerEntered();
	}

	public void OnPlayerDetectionExited(Node2D body)
	{
		if (!body.IsInGroup(GameConstants.PlayerGroup))
			return;

		OnPlayerExited();
	}

	private void OnPlayerEntered()
	{
		_playerInRange = true;

		SpawnOrRecallWorker();

		if (_state == CastleState.Destroyed)
			StartBuilding();
		else if (_state == CastleState.Building)
			_buildTimer.Start(); // resume building
	}

	private void OnPlayerExited()
	{
		_playerInRange = false;
		_buildTimer.Stop();
		SendWorkerBack();
	}
	private void SpawnOrRecallWorker()
	{
		if (WorkerScene == null)
			return;

		// If worker already exists and is valid, recall it to work point
		if (GodotObject.IsInstanceValid(_workerInstance))
		{
			_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
			_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
			return;
		}

		// Spawn new worker
		_workerInstance = WorkerScene.Instantiate<Worker>();
		GetParent().CallDeferred("add_child", _workerInstance);
		_workerInstance.GlobalPosition = _workerSpawnPoint.GlobalPosition;
		_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
	}
	private void StartBuilding()
	{
		_state = CastleState.Building;
		_sprite.Texture = ConstructionTexture;
		_buildTimer.Start();
	}
	private void OnBuildTick()
	{
		if (!_playerInRange || _state != CastleState.Building)
			return;

		_buildProgress += 1f;

		if (_buildProgress >= BuildTimeRequired)
			FinishBuilding();
	}
	private void FinishBuilding()
	{
		_state = CastleState.Built;
		_sprite.Texture = BuiltTexture;
		_buildTimer.Stop();
		SendWorkerBack();
	}
	private void SendWorkerBack()
	{
		if (!GodotObject.IsInstanceValid(_workerInstance))
			return;

		// tell worker to walk back to spawn point and despawn when arrived
		// don't null the reference - worker will be freed on despawn and IsInstanceValid will return false
		_workerInstance.MoveToAndDespawn(_workerSpawnPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerSpawnPoint.GlobalPosition);
	}
}
