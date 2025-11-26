using dTopDownShooter.Scripts;
using Godot;

public partial class Dynamite : Area2D
{
    [Export]
    public ushort Damage { get; set; } = 100;

    [Export]
    public float ThrowDuration { get; set; } = 0.3f;

    [Export]
    public float BlastRadius { get; set; } = 100f;

    public Vector2 StartPosition { get; set; }
    public Vector2 TargetPosition { get; set; }

    private AnimatedSprite2D _animation;
    private bool _hasExploded = false;
    private bool _showBlastRadius = false;

    public override void _Ready()
    {
        _animation = GetNode<AnimatedSprite2D>("Animation");

        // Add to dynamite group for cleanup on restart
        AddToGroup(GameConstants.DynamiteGroup);

        // Set starting position and begin throw
        GlobalPosition = StartPosition;
        StartThrow();
    }

    public override void _Draw()
    {
        if (_showBlastRadius)
        {
            // Draw subtle red circle showing blast radius
            DrawArc(Vector2.Zero, BlastRadius, 0, Mathf.Tau, 32, new Color(1, 0, 0, 0.25f), 1f);
        }
    }

    private void StartThrow()
    {
        var tween = CreateTween();
        tween.SetParallel(true);

        // Move to target position
        tween.TweenProperty(this, "global_position", TargetPosition, ThrowDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);

        // Rotate while flying
        tween.TweenProperty(_animation, "rotation", Mathf.Tau * 2, ThrowDuration);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(StartFuse));
    }

    private void StartFuse()
    {
        // Show blast radius indicator
        _showBlastRadius = true;
        QueueRedraw();

        // Play the fuse animation (cycles through 6 frames)
        _animation.Play("fuse");

        // When animation finishes, explode
        _animation.AnimationFinished += Explode;
    }

    private void Explode()
    {
        if (_hasExploded)
            return;

        _hasExploded = true;

        // Get all enemies in blast radius
        var enemies = GetTree().GetNodesInGroup(GameConstants.EnemiesGroup);
        foreach (Node2D enemy in enemies)
        {
            float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance <= BlastRadius)
            {
                if (enemy is Enemy enemyChar)
                {
                    enemyChar.TakeDamage(Damage);
                }
            }
        }

        // Visual explosion effect
        PlayExplosionEffect();
    }

    private void PlayExplosionEffect()
    {
        // Scale up and fade out
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_animation, "scale", Vector2.One * 3, 0.2f);
        tween.TweenProperty(_animation, "modulate", new Color(1, 0.5f, 0, 1), 0.1f);
        tween.SetParallel(false);
        tween.TweenProperty(_animation, "modulate", new Color(1, 1, 1, 0), 0.1f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}
