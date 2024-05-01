using Godot;

public partial class Bow : Node2D
{
    [Export]
    private PackedScene _arrowScene;
    private const float arrow_speed = 600f;
    private const float fire_rate = 1 / 5f;
    private float damage = 30f;

    private float time_until_next_fire = 0f;

    private AnimatedSprite2D arrowAnimation;

    public override void _Ready()
    {
        //arrowAnimation = GetNode<AnimatedSprite2D>("ArrowAnimations");
    }

    public override void _Process(double delta)
    {
        //arrowAnimation.Play("full");
        if (Input.IsActionPressed("fire") && time_until_next_fire > fire_rate)
        {
            var arrow = _arrowScene.Instantiate<RigidBody2D>();
            AnimatedSprite2D arrowAnimation = arrow.GetNode<AnimatedSprite2D>(new NodePath("ArrowAnimations"));
            arrowAnimation.Play("full");

            if (Input.IsActionPressed("left"))
            {
                arrowAnimation.RotationDegrees = 180;
            }
            if (Input.IsActionPressed("down"))
            {
                arrowAnimation.RotationDegrees = 90;
            }
            if (Input.IsActionPressed("up"))
            {
                arrowAnimation.RotationDegrees = -90;
            }
            //arrowAnimation.FlipH = playerFacing < 0;

            arrow.Rotation = GlobalRotation;
            arrow.Position = GlobalPosition;
            Vector2 move_input = Input.GetVector("left", "right", "up", "down");
            if (move_input == Vector2.Zero)
            {
                move_input = new Vector2(1, 0);
            }
            arrow.LinearVelocity = move_input * arrow_speed;

            GetTree().Root.AddChild(arrow);

            time_until_next_fire = 0f;
        }
        else
        {
            time_until_next_fire += (float)delta;
        }
    }
}
