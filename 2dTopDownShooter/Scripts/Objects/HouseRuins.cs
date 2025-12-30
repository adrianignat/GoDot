using dTopDownShooter.Scripts;
using Godot;

public partial class HouseRuins : Node2D
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

	private enum HouseState
	{
		Destroyed,
		Building,
		Built
	}
	private HouseState _state = HouseState.Destroyed;

	public bool IsBuilt => _state == HouseState.Built;

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

	public override void _Process(double delta)
	{
		var player = Game.Instance?.Player;
		if (player == null || _state == HouseState.Built)
			return;

		float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
		bool inRange = distance <= GameConstants.BuildDetectionRadius;

		if (inRange && !_playerInRange)
			OnPlayerEntered();
		else if (!inRange && _playerInRange)
			OnPlayerExited();
	}
	private void OnPlayerEntered()
	{
		_playerInRange = true;

		SpawnWorker();

		if (_state == HouseState.Destroyed)
			StartBuilding();
		else if (_state == HouseState.Building)
			_buildTimer.Start(); // resume building
	}

	private void OnPlayerExited()
	{
		_playerInRange = false;
		_buildTimer.Stop();
		SendWorkerBack();
	}
	private void SpawnWorker()
	{
		// check if worker exists and is still valid (not queued for deletion)
		if (GodotObject.IsInstanceValid(_workerInstance) || WorkerScene == null)
			return;

		_workerInstance = WorkerScene.Instantiate<Worker>();
		GetParent().AddChild(_workerInstance);
		_workerInstance.GlobalPosition = _workerSpawnPoint.GlobalPosition;
		// tell worker to move to work point
		_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
		// make the worker face the work point
		_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
	}
	private void StartBuilding()
	{
		_state = HouseState.Building;
		_sprite.Texture = ConstructionTexture;
		_buildTimer.Start();
	}
	private void OnBuildTick()
	{
		if (!_playerInRange || _state != HouseState.Building)
			return;

		_buildProgress += 1f;

		if (_buildProgress >= BuildTimeRequired)
			FinishBuilding();
	}
	private void FinishBuilding()
	{
		_state = HouseState.Built;
		_sprite.Texture = BuiltTexture;
		_buildTimer.Stop();
		SendWorkerBack();
	}
	private void SendWorkerBack()
	{
		if (!GodotObject.IsInstanceValid(_workerInstance))
			return;

		// tell worker to walk back to spawn point and despawn when arrived
		_workerInstance.MoveToAndDespawn(_workerSpawnPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerSpawnPoint.GlobalPosition);
		_workerInstance = null;
	}
}
