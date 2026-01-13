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
        const int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 targetPosition = CalculateRawTargetPosition();

            // Check if landing in water
            if (!Game.Instance.IsInNoSpawnZone(targetPosition))
            {
                return targetPosition;
            }

            // Try to find a valid position along the throw path
            Vector2 validPosition = FindValidPositionAlongPath(targetPosition);
            if (validPosition != Vector2.Zero)
            {
                return validPosition;
            }

            // If no valid position found, try a different target
        }

        // Fallback: throw near player (should always be valid)
        return _player.GlobalPosition + new Vector2(50, 0);
    }

    private Vector2 CalculateRawTargetPosition()
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

    private Vector2 FindValidPositionAlongPath(Vector2 targetPosition)
    {
        // Trace back from target to player to find last valid position
        Vector2 direction = (targetPosition - _player.GlobalPosition).Normalized();
        float totalDistance = _player.GlobalPosition.DistanceTo(targetPosition);
        const float stepSize = 20f;

        // Start from near the target and work backwards
        for (float dist = totalDistance - stepSize; dist > stepSize; dist -= stepSize)
        {
            Vector2 checkPos = _player.GlobalPosition + direction * dist;
            if (!Game.Instance.IsInNoSpawnZone(checkPos))
            {
                return checkPos;
            }
        }

        return Vector2.Zero; // No valid position found
    }

    protected override void InitializeSpawnedObject(Barrel barrel)
    {
        barrel.BlastRadius += BonusBlastRadius;
        barrel.StartPosition = _player.GlobalPosition;
        barrel.TargetPosition = _nextTargetPosition;
    }
}
