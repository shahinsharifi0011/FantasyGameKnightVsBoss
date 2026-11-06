using System;
using System.Collections.Generic;
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
    public NodePath HealthComponentPath { get; set; } = new("HealthComponent");

    [Export]
    public NodePath AttackHitboxPath { get; set; } = new("AttackHitbox");

    [Export]
    public float MoveSpeed { get; set; } = 180.0f;

    [Export]
    public float Gravity { get; set; } = 980.0f;

    [Export]
    public float JumpVelocity { get; set; } = -350.0f;

    private KnightStateMachine _stateMachine = null!;
    private AnimatedSprite2D _animatedSprite = null!;
    private HealthComponent _health = null!;
    private Area2D _attackHitbox = null!;
    private readonly HashSet<ulong> _hitBosses = new();
    private float _facingDirection = 1.0f;

    public float MoveInput => Input.GetAxis(MoveLeftAction, MoveRightAction);
    public bool IsBlocking => _stateMachine.CurrentState is BlockState;

    public override void _Ready()
    {
        _stateMachine = GetNode<KnightStateMachine>(StateMachinePath);
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpritePath);
        _health = GetNode<HealthComponent>(HealthComponentPath);
        _attackHitbox = GetNode<Area2D>(AttackHitboxPath);
        _health.Died += OnDied;
        _attackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
        _attackHitbox.Monitoring = false;
        AddToGroup("player");
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
            _facingDirection = -1.0f;
            _animatedSprite.FlipH = true;
        }
        else if (direction > 0.0f)
        {
            _facingDirection = 1.0f;
            _animatedSprite.FlipH = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsBlocking)
        {
            amount = Math.Max(1, amount / 2);
        }

        _health.TakeDamage(amount);
    }

    public void SetAttackActive(bool active)
    {
        if (!IsInsideTree() || (active && _health.IsDead))
        {
            return;
        }

        if (active)
        {
            _hitBosses.Clear();
            _attackHitbox.Position = new Vector2(_facingDirection * 58.0f, -5.0f);
        }

        _attackHitbox.Monitoring = active;
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

    private void OnAttackHitboxBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("boss") || !_hitBosses.Add(body.GetInstanceId()))
        {
            return;
        }

        HealthComponent targetHealth = body.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth is not null)
        {
            targetHealth.TakeDamage(1);
        }
    }

    private void OnDied()
    {
        SetAttackActive(false);
        Velocity = Vector2.Zero;
        _stateMachine.SetProcess(false);
        _stateMachine.SetPhysicsProcess(false);
        SetProcess(false);
        SetPhysicsProcess(false);

        SceneTreeTimer restartTimer = GetTree().CreateTimer(2.0);
        restartTimer.Timeout += () =>
        {
            if (IsInsideTree())
            {
                GetTree().ReloadCurrentScene();
            }
        };
    }
}
