using dTopDownShooter.Scripts;
using Godot;
using System;

public partial class Bow : Node2D
{
    private const float _arrowSpeed = 600f;

    [Export]
    private PackedScene _arrowScene;
    [Export]
    private float FireRate = 1f;
    [Export]
    private short Damage = 50;

    private float _timeUntilNextFire = 0f;

    private bool _arrowQueued = false;

    private AnimatedSprite2D _playerAnimation;
    private Player _player;

    public override void _Ready()
    {   
        _playerAnimation = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimations");
        _player = GetParent<Player>();
    }

    public override void _Process(double delta)
    {
        if (_timeUntilNextFire > FireRate && !_arrowQueued)
        {
            _arrowQueued = true;

            switch (_player.Moving)
            {
                case Direction.N:
                    _playerAnimation.Play("shoot_up");
                    break;
                case Direction.S:
                    _playerAnimation.Play("shoot_down");
                    break;
                case Direction.SE:
                case Direction.SW:
                    _playerAnimation.Play("shoot_down_forward");
                    break;
                case Direction.NE:
                case Direction.NW:
                    _playerAnimation.Play("shoot_up_forward");
                    break;
                default:
                    _playerAnimation.Play("shoot_forward");
                    break;
            }
        }
        else
        {
            _timeUntilNextFire += (float)delta;
        }

        if (_arrowQueued
            && _playerAnimation.Animation.ToString().Contains("shoot")
            && _playerAnimation.Frame == 6)
            AddArrow();
    }

    private RigidBody2D CreateNewArrow()
    {
        var arrow = _arrowScene.Instantiate<Arrow>();
        arrow.Damage = Damage;
        var arrowAnimation = arrow.GetChild<AnimatedSprite2D>(0);
        arrowAnimation.Play("full");

        arrow.Position = GlobalPosition;

        Vector2 move_input = Input.GetVector("left", "right", "up", "down");
        if (move_input == Vector2.Zero)
        {
            var x = _player.Facing == Direction.W ? -1 : 1;

            move_input = new Vector2(x, 0);
        }
        arrow.LinearVelocity = move_input * _arrowSpeed;

        
        var moving = DirectionHelper.GetMovingDirection(move_input);

        if (_player.Facing == Direction.W
            && _player.Moving != Direction.S
            && _player.Moving != Direction.SW)
            arrow.Position = new Vector2(GlobalPosition.X - 50, GlobalPosition.Y);

        if (moving == Direction.N) // Up
            arrowAnimation.RotationDegrees = -90;
        else if (moving == Direction.S) // Down
            arrowAnimation.RotationDegrees = 90;
        else if (moving == Direction.W) // Left
            arrowAnimation.RotationDegrees = 180;
        else if (moving == Direction.E) // Right
            arrowAnimation.RotationDegrees = 0;
        else if (moving == Direction.NW) // Up Left
            arrowAnimation.RotationDegrees = -135;
        else if (moving == Direction.NE) // Up Right
            arrowAnimation.RotationDegrees = -45;
        else if (moving == Direction.SW) // Down Left
            arrowAnimation.RotationDegrees = 135;
        else if (moving == Direction.SE) // Down Right
            arrowAnimation.RotationDegrees = 45;

        return arrow;
    }

    private Action AddArrow()
    {
        var arrow = CreateNewArrow();

        GetTree().Root.AddChild(arrow);

        _playerAnimation.Play("idle");

        _timeUntilNextFire = 0f;
        _arrowQueued = false;
        return null;
    }
}
