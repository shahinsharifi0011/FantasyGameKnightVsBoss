using System;
using Godot;

public partial class KnightController : CharacterBody2D
{
    public const string MoveLeftAction = "move_left";
    public const string MoveRightAction = "move_right";
    public const string JumpAction = "jump";
    public const string AttackAction = "attack";
    public const string BlockAction = "block";

    [Export]
    public NodePath StateMachinePath { get; set; } = new("StateMachine");

    [Export]
    public NodePath AnimatedSpritePath { get; set; } = new("AnimatedSprite2D");

    [Export]
    public float MoveSpeed { get; set; } = 180.0f;

    [Export]
    public float Gravity { get; set; } = 980.0f;

    [Export]
    public float JumpVelocity { get; set; } = -350.0f;

    private KnightStateMachine _stateMachine = null!;
    private AnimatedSprite2D _animatedSprite = null!;

    public float MoveInput => Input.GetAxis(MoveLeftAction, MoveRightAction);

    public override void _Ready()
    {
        _stateMachine = GetNode<KnightStateMachine>(StateMachinePath);
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpritePath);
    }

    public override void _PhysicsProcess(double delta)
    {
        _stateMachine.PhysicsUpdate(delta);

        if (!IsOnFloor())
        {
            Velocity += Vector2.Down * Gravity * (float)delta;
        }
        else if (Velocity.Y > 0.0f)
        {
            Velocity = new Vector2(Velocity.X, 0.0f);
        }

        MoveAndSlide();
    }

    public void SetHorizontalMovement(float direction)
    {
        Velocity = new Vector2(direction * MoveSpeed, Velocity.Y);

        if (direction < 0.0f)
        {
            _animatedSprite.FlipH = true;
        }
        else if (direction > 0.0f)
        {
            _animatedSprite.FlipH = false;
        }
    }

    public void PlayAnimation(string animationName)
    {
        StringName animation = new(animationName);
        SpriteFrames spriteFrames = _animatedSprite.SpriteFrames
            ?? throw new InvalidOperationException(
                $"{nameof(AnimatedSprite2D.SpriteFrames)} is not assigned.");

        if (!spriteFrames.HasAnimation(animation))
        {
            throw new InvalidOperationException(
                $"The AnimatedSprite2D does not have an animation named '{animationName}'.");
        }

        _animatedSprite.Play(animation);
    }

    public void StopHorizontalMovement()
    {
        Velocity = new Vector2(0.0f, Velocity.Y);
    }

    public bool TryJump()
    {
        if (!IsOnFloor())
        {
            return false;
        }

        Velocity = new Vector2(Velocity.X, JumpVelocity);
        return true;
    }
}
