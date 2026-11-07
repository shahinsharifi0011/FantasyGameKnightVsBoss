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
    private bool _isDead;
    private bool _deathSequenceFinished;

    public float MoveInput => Input.GetAxis(MoveLeftAction, MoveRightAction);
    public bool IsBlocking =>
        Input.IsActionPressed(BlockAction) || _stateMachine.CurrentState is BlockState;

    public override void _Ready()
    {
        _stateMachine = GetNode<KnightStateMachine>(StateMachinePath);
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpritePath);
        _health = GetNode<HealthComponent>(HealthComponentPath);
        _attackHitbox = GetNode<Area2D>(AttackHitboxPath);
        _health.Died += OnDied;
        _animatedSprite.AnimationFinished += OnDeathAnimationFinished;
        _attackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
        _attackHitbox.Monitoring = false;
        AddToGroup("Player");
        AddToGroup("player");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
        {
            return;
        }

        _stateMachine.PhysicsUpdate(delta);
        if (_attackHitbox.Monitoring)
        {
            foreach (Node2D body in _attackHitbox.GetOverlappingBodies())
            {
                TryDamageBoss(body);
            }
        }

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
        if (_isDead)
        {
            return;
        }

        if (IsBlocking)
        {
            return;
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
        TryDamageBoss(body);
    }

    private void TryDamageBoss(Node2D body)
    {
        if (!body.IsInGroup("Boss") || !_hitBosses.Add(body.GetInstanceId()))
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
        if (_isDead)
        {
            return;
        }

        _stateMachine.TransitionTo(_stateMachine.GetState<DeathState>());
    }

    public void BeginDeath()
    {
        _isDead = true;
        SetAttackActive(false);
        Velocity = Vector2.Zero;
        _stateMachine.SetProcess(false);
        _stateMachine.SetPhysicsProcess(false);
        SetPhysicsProcess(false);

        SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);
        DisableCollisionsRecursively(this);
        _animatedSprite.SpriteFrames!.SetAnimationLoopMode(
            new StringName("death"),
            SpriteFrames.LoopMode.None);
        PlayAnimation("death");
    }

    private void OnDeathAnimationFinished()
    {
        if (!_isDead
            || _deathSequenceFinished
            || _animatedSprite.Animation != new StringName("death"))
        {
            return;
        }

        _deathSequenceFinished = true;
        Godot.Collections.Array<Node> gameOverScreens = GetTree().GetNodesInGroup("GameOverUI");
        if (gameOverScreens.Count > 0
            && gameOverScreens[0] is GameOverScreen gameOverScreen)
        {
            gameOverScreen.ShowGameOver();
            return;
        }

        GD.PushError("No GameOverScreen was found; the player remains dead.");
    }

    private void DisableCollisionsRecursively(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is CollisionShape2D collisionShape)
            {
                collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            }
            else if (child is CollisionPolygon2D collisionPolygon)
            {
                collisionPolygon.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
            }
            else if (child is Area2D area)
            {
                area.SetDeferred(Area2D.PropertyName.Monitoring, false);
                area.SetDeferred(Area2D.PropertyName.Monitorable, false);
                area.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
                area.SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);
            }

            DisableCollisionsRecursively(child);
        }
    }
}
