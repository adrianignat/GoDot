using Godot;

public partial class CuttingTree : Node2D
{
    private AnimatedSprite2D _sprite;
    private Area2D _area;
    private Timer _cutTimer;
    private StaticBody2D _staticBody;

    private bool _isCut = false;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _area = GetNode<Area2D>("Area2D");
        _staticBody = GetNode<StaticBody2D>("StaticBody2D");

        // Create timer in code (keeps scene clean)
        _cutTimer = new Timer
        {
            OneShot = true,
            WaitTime = 3.0
        };
        AddChild(_cutTimer);

        _area.BodyEntered += OnBodyEntered;
        _cutTimer.Timeout += OnCutFinished;

        _sprite.Play("idle");
    }

    private void OnBodyEntered(Node body)
    {
        if (_isCut)
            return;

        if (body is not Player)
            return;

        StartCutting();
    }

    private void StartCutting()
    {
        _sprite.Play("cutting");
        _cutTimer.Start();
    }

    private void OnCutFinished()
    {
        _isCut = true;

        _sprite.Play("stump");

        // Disable interactions
        _area.Monitoring = false;

        // Remove blocking collision (tree no longer blocks player)
        _staticBody.QueueFree();
    }
}
