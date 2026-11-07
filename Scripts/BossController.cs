#nullable enable
using System;
using Godot;

public partial class BossController : CharacterBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; set; } = new("HealthComponent");

    [Export]
    public NodePath AnimatedSpritePath { get; set; } = new("AnimatedSprite2D");

    [Export]
    public NodePath BossHitboxPath { get; set; } = new("BossHitbox");

    [Export]
    public float MoveSpeed { get; set; } = 90.0f;

    [Export]
    public float Gravity { get; set; } = 980.0f;

    [Export]
    public float AttackRange { get; set; } = 100.0f;

    [Export]
    public float AttackCooldown { get; set; } = 1.4f;

    [Export]
    public float AttackWindup { get; set; } = 0.45f;

    [Export]
    public float AttackHitboxDuration { get; set; } = 0.2f;

    [Export]
    public int AttackDamage { get; set; } = 1;

    private HealthComponent _health = null!;
    private AnimatedSprite2D _animatedSprite = null!;
    private Area2D _bossHitbox = null!;
    private KnightController? _player;
    private double _attackCooldownRemaining;
    private double _attackWindupRemaining;
    private double _attackHitboxRemaining;
    private bool _isAttacking;
    private bool _isHitboxActive;
    private bool _isDead;

    public override void _Ready()
    {
        AddToGroup("Boss");
        _health = GetNode<HealthComponent>(HealthComponentPath);
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpritePath);
        _bossHitbox = GetNode<Area2D>(BossHitboxPath);

        _health.Died += OnDied;
        _animatedSprite.AnimationFinished += OnAnimationFinished;
        _bossHitbox.BodyEntered += OnBossHitboxBodyEntered;
        _bossHitbox.Monitoring = false;
        PlayAnimation("idle");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
        {
            return;
        }

        if (_player is null || !GodotObject.IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as KnightController;
        }

        if (_player is null)
        {
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
        _animatedSprite.FlipH = toPlayer.X < 0.0f;
        ApplyGravity(delta);

        if (toPlayer.Length() > AttackRange)
        {
            CancelAttack();
            Velocity = new Vector2(Mathf.Sign(toPlayer.X) * MoveSpeed, Velocity.Y);
            PlayAnimation(HasAnimation("walk") ? "walk" : "idle");
            MoveAndSlide();
            return;
        }

        Velocity = new Vector2(0.0f, Velocity.Y);
        MoveAndSlide();

        if (_isAttacking)
        {
            UpdateAttack(delta);
            return;
        }

        _attackCooldownRemaining = Math.Max(0.0, _attackCooldownRemaining - delta);
        if (_attackCooldownRemaining <= 0.0)
        {
            StartAttack();
        }
        else
        {
            PlayAnimation("idle");
        }
    }

    private void ApplyGravity(double delta)
    {
        if (!IsOnFloor())
        {
            Velocity += Vector2.Down * Gravity * (float)delta;
        }
        else if (Velocity.Y > 0.0f)
        {
            Velocity = new Vector2(Velocity.X, 0.0f);
        }
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _isHitboxActive = false;
        _attackWindupRemaining = Math.Max(0.0, AttackWindup);
        _attackHitboxRemaining = Math.Max(0.0, AttackHitboxDuration);
        _attackCooldownRemaining = Math.Max(0.0, AttackCooldown);
        _bossHitbox.Monitoring = false;
        PlayAnimation("attack");
    }

    private void UpdateAttack(double delta)
    {
        if (!_isHitboxActive)
        {
            _attackWindupRemaining -= delta;
            if (_attackWindupRemaining <= 0.0)
            {
                _isHitboxActive = true;
                _bossHitbox.Monitoring = true;
            }

            return;
        }

        _attackHitboxRemaining -= delta;
        if (_attackHitboxRemaining <= 0.0)
        {
            _bossHitbox.Monitoring = false;
            _isHitboxActive = false;
            _isAttacking = false;
            PlayAnimation("idle");
        }
    }

    private void CancelAttack()
    {
        _bossHitbox.Monitoring = false;
        _isHitboxActive = false;
        _isAttacking = false;
    }

    private void OnBossHitboxBodyEntered(Node2D body)
    {
        if (!_isDead
            && body.IsInGroup("player")
            && body is KnightController player)
        {
            player.TakeDamage(AttackDamage);
        }
    }

    private void OnDied()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Velocity = Vector2.Zero;
        CancelAttack();
        SetPhysicsProcess(false);
        SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);
        DisableCollisionsRecursively(this);
        PlayAnimation("death");
    }

    private void OnAnimationFinished()
    {
        if (_isDead && _animatedSprite.Animation == new StringName("death"))
        {
            QueueFree();
        }
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

    private bool HasAnimation(string animationName)
    {
        return _animatedSprite.SpriteFrames?.HasAnimation(new StringName(animationName)) ?? false;
    }

    private void PlayAnimation(string animationName)
    {
        if (!HasAnimation(animationName))
        {
            throw new InvalidOperationException(
                $"The boss AnimatedSprite2D has no animation named '{animationName}'.");
        }

        if (_animatedSprite.Animation != new StringName(animationName))
        {
            _animatedSprite.Play(animationName);
        }
    }
}
