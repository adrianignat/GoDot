using dTopDownShooter.Scripts;
using Godot;

public partial class GoldmineRuins : Node2D
{
	[Signal]
	public delegate void BuiltEventHandler(GoldmineRuins ruins);

	[Signal]
	public delegate void BuildProgressChangedEventHandler(GoldmineRuins ruins, float currentProgress, float requiredProgress);

	[Signal]
	public delegate void GoldExtractedEventHandler(GoldmineRuins ruins, int amountExtracted, int remainingAmount);

	[Signal]
	public delegate void DepletedEventHandler(GoldmineRuins ruins);

	[Export] public Texture2D DestroyedTexture;
	[Export] public Texture2D ConstructionTexture;
	[Export] public Texture2D BuiltTexture;

	[Export] public PackedScene WorkerScene;
	[Export] public float BuildTimeRequired = 5f;
	[Export] public int TotalGoldAvailable = 12;
	[Export] public int GoldPerTick = 1;

	private Sprite2D _sprite;
	private Timer _buildTimer;
	private Marker2D _workerSpawnPoint;
	private Marker2D _workerWorkPoint;

	private Worker _workerInstance;
	private float _buildProgress = 0f;
	private bool _playerInRange = false;
	private int _remainingGold;
	private bool _isDepleted = false;

	private enum GoldmineState
	{
		Destroyed,
		Building,
		Built
	}
	private GoldmineState _state = GoldmineState.Destroyed;

	public bool IsBuilt => _state == GoldmineState.Built;
	public bool IsDepleted => _isDepleted;
	public int RemainingGold => _remainingGold;
	public int TotalGold => TotalGoldAvailable;
	public float BuildProgress => _buildProgress;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_buildTimer = GetNode<Timer>("Timer");
		_workerSpawnPoint = GetNode<Marker2D>("WorkerSpawnPoint");
		_workerWorkPoint = GetNode<Marker2D>("WorkerWorkPoint");

		_buildTimer.Timeout += OnBuildTick;
		_buildTimer.WaitTime = 1.0;

		_sprite.Texture = DestroyedTexture;
		_remainingGold = TotalGoldAvailable;

		if (WorkerScene == null)
			WorkerScene = GD.Load<PackedScene>("res://Entities/Characters/worker.tscn");
	}

	public void OnPlayerDetectionEntered(Node2D body)
	{
		if (!body.IsInGroup(GameConstants.PlayerGroup))
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
		if (_isDepleted)
			return;

		_playerInRange = true;
		SpawnOrRecallWorker();

		if (_state == GoldmineState.Destroyed)
			StartBuilding();
		else if (_state == GoldmineState.Building || _state == GoldmineState.Built)
			_buildTimer.Start();
	}

	private void OnPlayerExited()
	{
		_playerInRange = false;
		_buildTimer.Stop();
		SendWorkerBack();
	}

	private Vector2 GetSafeWorkerSpawnPosition()
	{
		Vector2 preferred = _workerSpawnPoint.GlobalPosition;
		if (!Game.Instance.IsInNoSpawnZone(preferred))
			return preferred;

		Vector2[] offsets =
		{
			Vector2.Zero,
			new(48, 0),
			new(-48, 0),
			new(0, 48),
			new(0, -48),
			new(64, 64),
			new(-64, 64),
			new(64, -64),
			new(-64, -64)
		};

		foreach (var offset in offsets)
		{
			Vector2 candidate = _workerWorkPoint.GlobalPosition + offset;
			if (!Game.Instance.IsInNoSpawnZone(candidate))
				return candidate;
		}

		return GlobalPosition;
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
		_workerInstance.GlobalPosition = GetSafeWorkerSpawnPosition();
		_workerInstance.MoveTo(_workerWorkPoint.GlobalPosition);
		_workerInstance.FaceTowards(_workerWorkPoint.GlobalPosition);
	}

	private void StartBuilding()
	{
		_state = GoldmineState.Building;
		_sprite.Texture = ConstructionTexture;
		_buildProgress = 0f;
		EmitSignal(SignalName.BuildProgressChanged, this, _buildProgress, BuildTimeRequired);
		_buildTimer.Start();
	}

	private void OnBuildTick()
	{
		if (!_playerInRange)
			return;

		if (_state == GoldmineState.Building)
		{
			_buildProgress += 1f;
			EmitSignal(SignalName.BuildProgressChanged, this, Mathf.Min(_buildProgress, BuildTimeRequired), BuildTimeRequired);
			if (_buildProgress >= BuildTimeRequired)
				FinishBuilding();
			return;
		}

		if (_state == GoldmineState.Built)
			ExtractGold();
	}

		private void FinishBuilding()
	{
		_state = GoldmineState.Built;
		_sprite.Texture = BuiltTexture;
		EmitSignal(SignalName.BuildProgressChanged, this, BuildTimeRequired, BuildTimeRequired);
		EmitSignal(SignalName.Built, this);
	}

	private void ExtractGold()
	{
		if (_isDepleted)
			return;

		int extracted = Mathf.Min(GoldPerTick, _remainingGold);
		if (extracted <= 0)
		{
			DepleteMine();
			return;
		}

		_remainingGold -= extracted;
		Game.Instance.EmitSignal(Game.SignalName.GoldAcquired, (ushort)extracted);
		EmitSignal(SignalName.GoldExtracted, this, extracted, _remainingGold);

		if (_remainingGold <= 0)
			DepleteMine();
	}

	private void DepleteMine()
	{
		if (_isDepleted)
			return;

		_isDepleted = true;
		_buildTimer.Stop();
		SendWorkerBack();
		EmitSignal(SignalName.Depleted, this);
	}

	private void SendWorkerBack()
	{
		if (!GodotObject.IsInstanceValid(_workerInstance))
			return;

		_workerInstance.MoveToAndDespawn(GetSafeWorkerSpawnPosition());
		_workerInstance.FaceTowards(GetSafeWorkerSpawnPosition());
	}
}
