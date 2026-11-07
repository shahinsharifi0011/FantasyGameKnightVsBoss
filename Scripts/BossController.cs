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
    private AnimatedSprite2D? _animatedSprite;
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
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath)
            ?? FindChild("AnimatedSprite2D", true, false) as AnimatedSprite2D;
        if (_animatedSprite is null)
        {
            GD.PushWarning(
                $"'{Name}' has no AnimatedSprite2D at '{AnimatedSpritePath}'. "
                + "Boss movement and hit detection will continue without animations.");
        }

        _bossHitbox = GetNodeOrNull<Area2D>(BossHitboxPath)
            ?? FindChild("BossHitbox", true, false) as Area2D
            ?? CreateFallbackHitbox();

        _health.Died += OnDied;
        if (_animatedSprite is not null)
        {
            _animatedSprite.AnimationFinished += OnAnimationFinished;
        }
        _bossHitbox.BodyEntered += OnBossHitboxBodyEntered;
        SetHitboxActive(false);
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
        if (_animatedSprite is not null)
        {
            _animatedSprite.FlipH = toPlayer.X < 0.0f;
        }
        ApplyGravity(delta);

        if (Mathf.Abs(toPlayer.X) > AttackRange)
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
        SetHitboxActive(false);
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
                SetHitboxActive(true);
            }

            return;
        }

        _attackHitboxRemaining -= delta;
        if (_attackHitboxRemaining <= 0.0)
        {
            SetHitboxActive(false);
            _isHitboxActive = false;
            _isAttacking = false;
            PlayAnimation("idle");
        }
    }

    private void CancelAttack()
    {
        SetHitboxActive(false);
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
        if (_animatedSprite is null || !HasAnimation("death"))
        {
            QueueFree();
            return;
        }

        PlayAnimation("death");
    }

    private void OnAnimationFinished()
    {
        if (_isDead
            && _animatedSprite is not null
            && _animatedSprite.Animation == new StringName("death"))
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
        return _animatedSprite?.SpriteFrames?.HasAnimation(new StringName(animationName)) ?? false;
    }

    private void PlayAnimation(string animationName)
    {
        AnimatedSprite2D? animatedSprite = _animatedSprite;
        if (animatedSprite is null)
        {
            return;
        }

        if (!HasAnimation(animationName))
        {
            return;
        }

        if (animatedSprite.Animation != new StringName(animationName))
        {
            animatedSprite.Play(animationName);
        }
    }

    private void SetHitboxActive(bool active)
    {
        _bossHitbox.SetDeferred(Area2D.PropertyName.Monitoring, active);
    }

    private Area2D CreateFallbackHitbox()
    {
        Area2D hitbox = new()
        {
            Name = "BossHitbox",
            CollisionLayer = 0,
            CollisionMask = 1
        };
        RectangleShape2D shape = new()
        {
            Size = new Vector2(AttackRange, 70.0f)
        };
        CollisionShape2D collisionShape = new()
        {
            Shape = shape
        };
        hitbox.AddChild(collisionShape);
        AddChild(hitbox);
        return hitbox;
    }
}
