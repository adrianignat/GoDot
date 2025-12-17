using dTopDownShooter.Scripts;
using Godot;

public partial class TowerRuins : Node2D
{
    // === EXPORTED TEXTURES ===
    [Export] public Texture2D DestroyedTexture;
    [Export] public Texture2D ConstructionTexture;
    [Export] public Texture2D BuiltTexture;

    private Sprite2D _sprite;
    private Timer _buildTimer;

    private enum TowerState
    {
        Destroyed,
        Building,
        Built
    }

    private TowerState _state = TowerState.Destroyed;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _buildTimer = GetNode<Timer>("Timer");

        _buildTimer.Timeout += OnBuildFinished;

        // Start as destroyed
        _sprite.Texture = DestroyedTexture;
    }

    // Call this when the player interacts with the tower
    public void Interact()
    {
        if (_state != TowerState.Destroyed)
            return;

        _state = TowerState.Building;
        _sprite.Texture = ConstructionTexture;

        // Build duration in seconds
        _buildTimer.Start(3.0);
    }

    private void OnBuildFinished()
    {
        _state = TowerState.Built;
        _sprite.Texture = BuiltTexture;
    }
}
