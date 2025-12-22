using Godot;
using System;

public partial class HouseRuins : Node2D
{
    [Export] public Texture2D DestroyedTexture;
    [Export] public Texture2D ConstructionTexture;
    [Export] public Texture2D BuiltTexture;

    [Export] public PackedScene WorkerScene;
    [Export] public float BuildTimeRequired = 5f;

    private Sprite2D _sprite;
    private Timer _buildTimer;
    private Area2D _area;
    private Marker2D _workerSpawnPoint;

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
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _buildTimer = GetNode<Timer>("Timer");
        _area = GetNode<Area2D>("Area2D");
        _workerSpawnPoint = GetNode<Marker2D>("Marker2D");

        _area.BodyEntered += OnBodyEntered;
        _area.BodyExited += OnBodyExited;
        _buildTimer.Timeout += OnBuildTick;

        _sprite.Texture = DestroyedTexture;
        // fallback: load worker scene if not assigned in the inspector
        if (WorkerScene == null)
        {
            WorkerScene = GD.Load<PackedScene>("res://Entities/Characters/worker.tscn");
        }
    }
    private void OnBodyEntered(Node body)
    {
        if (body is not Player)
            return;

        _playerInRange = true;

        // ensure worker spawns when player enters the area
        SpawnWorker();

        if (_state == HouseState.Destroyed)
            StartBuilding();
    }
    private void OnBodyExited(Node body)
    {
        if (body is not Player)
            return;

        _playerInRange = false;
    }
    private void SpawnWorker()
    {
        if (_workerInstance != null)
            return;

        _workerInstance = WorkerScene.Instantiate<Worker>();
        GetParent().AddChild(_workerInstance);
        //tell worker where to go
        _workerInstance.MoveTo(GlobalPosition);
        // make the worker face the tower
        _workerInstance.FaceTowards(GlobalPosition);
    }
    private void StartBuilding()
    {
        _state = HouseState.Building;
        _buildProgress = 0f;
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
        DespawnWorker();
    }
    private void DespawnWorker()
    {
        if (_workerInstance == null)
            return;

        _workerInstance.Despawn();
        _workerInstance = null;
    }
    
}
