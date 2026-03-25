using dTopDownShooter.Scripts;
using Godot;

public partial class TowerRuins : Node2D
{
	[Signal]
	public delegate void BuiltEventHandler(TowerRuins ruins);

	[Signal]
	public delegate void BuildProgressChangedEventHandler(TowerRuins ruins, float currentProgress, float requiredProgress);

	[Export] public Texture2D DestroyedTexture;
	[Export] public Texture2D ConstructionTexture;
	[Export] public Texture2D BuiltTexture;

	[Export] public PackedScene WorkerScene;
	[Export] public PackedScene ArcherScene;
	[Export] public float BuildTimeRequired = 5f;

	private Sprite2D _sprite;
	private Timer _buildTimer;
	private Marker2D _workerSpawnPoint;
	private Marker2D _workerWorkPoint;
	private Marker2D _archerSpawnPoint;

	private Worker _workerInstance;
	private TowerArcher _archerInstance;
	private float _buildProgress = 0f;
	private bool _playerInRange = false;

	private enum TowerState
	{
		Destroyed,
		Building,
		Built
	}
	private TowerState _state = TowerState.Destroyed;

	public bool IsBuilt => _state == TowerState.Built;
	public float CurrentBuildProgress => _buildProgress;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_buildTimer = GetNode<Timer>("Timer");
		_workerSpawnPoint = GetNode<Marker2D>("WorkerSpawnPoint");
		_workerWorkPoint = GetNode<Marker2D>("WorkerWorkPoint");
		_archerSpawnPoint = GetNode<Marker2D>("ArcherSpawnPoint");

		_buildTimer.Timeout += OnBuildTick;
		_buildTimer.WaitTime = 1.0;

		_sprite.Texture = DestroyedTexture;

		if (WorkerScene == null)
			WorkerScene = GD.Load<PackedScene>("res://Entities/Characters/worker.tscn");

		if (ArcherScene == null)
			ArcherScene = GD.Load<PackedScene>("res://Entities/Characters/archer.tscn");
	}

	public void OnPlayerDetectionEntered(Node2D body)
	{
		if (!body.IsInGroup(GameConstants.PlayerGroup) || _state == TowerState.Built)
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

		if (_state == TowerState.Destroyed)
			StartBuilding();
		else if (_state == TowerState.Building)
			_buildTimer.Start();
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

		if (GodotObject.IsInstanceValid(_workerInstance))
		{
			_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
			_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
			return;
		}

		_workerInstance = WorkerScene.Instantiate<Worker>();
		GetParent().CallDeferred("add_child", _workerInstance);
		_workerInstance.GlobalPosition = _workerSpawnPoint.GlobalPosition;
		_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
	}

	private void StartBuilding()
	{
		_state = TowerState.Building;
		_sprite.Texture = ConstructionTexture;
		_buildProgress = 0f;
		EmitSignal(SignalName.BuildProgressChanged, this, _buildProgress, BuildTimeRequired);
		_buildTimer.Start();
	}

	private void OnBuildTick()
	{
		if (!_playerInRange || _state != TowerState.Building)
			return;

		_buildProgress += 1f;
		EmitSignal(SignalName.BuildProgressChanged, this, Mathf.Min(_buildProgress, BuildTimeRequired), BuildTimeRequired);

		if (_buildProgress >= BuildTimeRequired)
			FinishBuilding();
	}

	private void FinishBuilding()
	{
		_state = TowerState.Built;
		_sprite.Texture = BuiltTexture;
		_buildTimer.Stop();
		SendWorkerBack();
		SpawnArcher();
		EmitSignal(SignalName.BuildProgressChanged, this, BuildTimeRequired, BuildTimeRequired);
		EmitSignal(SignalName.Built, this);
	}

	private void SendWorkerBack()
	{
		if (!GodotObject.IsInstanceValid(_workerInstance))
			return;

		_workerInstance.MoveToAndDespawn(_workerSpawnPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerSpawnPoint.GlobalPosition);
	}

	private void SpawnArcher()
	{
		if (ArcherScene == null || GodotObject.IsInstanceValid(_archerInstance))
			return;

		_archerInstance = ArcherScene.Instantiate<TowerArcher>();
		AddChild(_archerInstance);
		_archerInstance.Position = _archerSpawnPoint.Position;
	}
}
