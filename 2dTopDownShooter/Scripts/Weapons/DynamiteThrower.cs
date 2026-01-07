using dTopDownShooter.Scripts;
using dTopDownShooter.Scripts.Spawners;
using Godot;

public partial class DynamiteThrower : Spawner<Barrel>
{
    [Export]
    public float ThrowRadius = GameConstants.PlayerDynamiteThrowRadius;

    [Export]
    public float MaxThrowDistance = GameConstants.PlayerDynamiteMaxThrowDistance;

    /// <summary>
    /// Additional blast radius added per upgrade level.
    /// </summary>
    public float BonusBlastRadius { get; set; } = 0f;

    private Player _player;
    private Vector2 _nextTargetPosition;

    public override void _Ready()
    {
        _player = GetParent<Player>();
    }

    public override bool CanSpawn()
    {
        // Only spawn if we have the upgrade (ObjectsPerSecond > 0)
        return ObjectsPerSecond > 0;
    }

    public override Vector2 GetLocation()
    {
        // Barrel starts at player position
        // Store target for InitializeSpawnedObject
        _nextTargetPosition = GetTargetPosition();
        return _player.GlobalPosition;
    }

    private Vector2 GetTargetPosition()
    {
        Vector2 targetPosition;

        // Throw toward a random enemy, or random direction if no enemies
        var enemies = GetTree().GetNodesInGroup("enemies");

        if (enemies.Count > 0)
        {
            // Pick a random enemy and throw near it
            var randomEnemy = (Node2D)enemies[GD.RandRange(0, enemies.Count - 1)];
            targetPosition = randomEnemy.GlobalPosition;
        }
        else
        {
            // No enemies - throw in a random direction
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ThrowRadius;
            targetPosition = _player.GlobalPosition + offset;
        }

        // Clamp to max throw distance
        Vector2 direction = targetPosition - _player.GlobalPosition;
        float distance = direction.Length();
        if (distance > MaxThrowDistance)
        {
            targetPosition = _player.GlobalPosition + direction.Normalized() * MaxThrowDistance;
        }

        return targetPosition;
    }

    protected override void InitializeSpawnedObject(Barrel barrel)
    {
        barrel.BlastRadius += BonusBlastRadius;
        barrel.StartPosition = _player.GlobalPosition;
        barrel.TargetPosition = _nextTargetPosition;
    }
}
