using dTopDownShooter.Scripts;
using Godot;
using System;

public partial class Bow : Node2D
{
    [Export]
    private PackedScene _arrowScene;
    private const float arrow_speed = 600f;
    private const float fire_rate = 1f;
    private float damage = 30f;

    private float time_until_next_fire = 0f;

    private bool arrowQueued = false;

    private AnimatedSprite2D playerAnimation;
    private Player player;

    int animationFinishedCount = 0;

    public override void _Ready()
    {
        //arrowAnimation = GetNode<AnimatedSprite2D>("ArrowAnimations");
        playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
        player = GetParent<Player>();
    }

    public override void _Process(double delta)
    {
        if (time_until_next_fire > fire_rate && !arrowQueued)
        {
            arrowQueued = true;

            switch (player.Moving)
            {
                case Direction.N:
                    playerAnimation.Play("shoot_up");
                    break;
                case Direction.S:
                    playerAnimation.Play("shoot_down");
                    break;
                default:
                    playerAnimation.Play("shoot_forward");
                    break;
            }
        }
        else
        {
            time_until_next_fire += (float)delta;
        }

        if (arrowQueued
            && playerAnimation.Animation.ToString().Contains("shoot")
            && playerAnimation.Frame == 6)
            AddArrow();
    }

    private RigidBody2D CreateNewArrow()
    {
        var arrow = _arrowScene.Instantiate<RigidBody2D>();
        var arrowAnimation = arrow.GetChild<AnimatedSprite2D>(0);
        arrowAnimation.Play("full");

        arrow.Position = GlobalPosition;

        Vector2 move_input = Input.GetVector("left", "right", "up", "down");
        if (move_input == Vector2.Zero)
        {
            var x = player.Facing == Direction.W ? -1 : 1;
            
            move_input = new Vector2(x, 0);
        }
        arrow.LinearVelocity = move_input * arrow_speed;

        if (player.Facing == Direction.W
            && player.Moving != Direction.S
            && player.Moving != Direction.SW)
            arrow.Position = new Vector2(GlobalPosition.X - 50, GlobalPosition.Y);

        if (player.Moving == Direction.N) // Up
            arrowAnimation.RotationDegrees = -90;
        else if (player.Moving == Direction.S) // Down
            arrowAnimation.RotationDegrees = 90;
        else if (player.Moving == Direction.W) // Left
            arrowAnimation.RotationDegrees = 180;
        else if (player.Moving == Direction.E) // Right
            arrowAnimation.RotationDegrees = 0;
        else if (player.Moving == Direction.NW) // Up Left
            arrowAnimation.RotationDegrees = -135;
        else if (player.Moving == Direction.NE) // Up Right
            arrowAnimation.RotationDegrees = -45;
        else if (player.Moving == Direction.SW) // Down Left
            arrowAnimation.RotationDegrees = 135;
        else if (player.Moving == Direction.SE) // Down Right
            arrowAnimation.RotationDegrees = 45;

        return arrow;
    }

    private Action AddArrow()
    {
        var arrow = CreateNewArrow();

        GetTree().Root.AddChild(arrow);

        playerAnimation.Play("idle");

        time_until_next_fire = 0f;
        arrowQueued = false;
        return null;
    }
}
