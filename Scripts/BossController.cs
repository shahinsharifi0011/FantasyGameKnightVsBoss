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
    private bool _attackHasHitPlayer;
    private bool _usingFallbackHitbox;
    private bool _isDead;
    private bool _deathSequenceFinished;

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
        _bossHitbox.CollisionLayer = 0;
        _bossHitbox.CollisionMask = 1;

        _health.Died += OnDied;
        if (_animatedSprite is not null)
        {
            _animatedSprite.AnimationFinished += OnAnimationFinished;
        }
        _bossHitbox.BodyEntered += OnBossHitboxBodyEntered;
        SetHitboxActive(false);
        if (!HasAnimation("idle") || !HasAnimation("attack") || !HasAnimation("death"))
        {
            GD.PushWarning(
                $"'{Name}' should have non-looping 'attack' and 'death' animations, "
                + "plus an 'idle' animation, on its AnimatedSprite2D.");
        }

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
        _attackHasHitPlayer = false;
        if (_usingFallbackHitbox && _player is not null)
        {
            float direction = Mathf.Sign(_player.GlobalPosition.X - GlobalPosition.X);
            _bossHitbox.Position = new Vector2(direction * AttackRange * 0.5f, -10.0f);
        }

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

        foreach (Node2D body in _bossHitbox.GetOverlappingBodies())
        {
            TryDamagePlayer(body);
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
        TryDamagePlayer(body);
    }

    private void TryDamagePlayer(Node2D body)
    {
        if (_isDead || !_isAttacking || !_isHitboxActive || _attackHasHitPlayer)
        {
            return;
        }

        if (body.IsInGroup("player") && body is KnightController player)
        {
            _attackHasHitPlayer = true;
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
            CompleteDeathSequence();
            return;
        }

        _animatedSprite.SpriteFrames!.SetAnimationLoopMode(
            new StringName("death"),
            SpriteFrames.LoopMode.None);
        PlayAnimation("death");
    }

    private void OnAnimationFinished()
    {
        if (_isDead
            && _animatedSprite is not null
            && _animatedSprite.Animation == new StringName("death"))
        {
            CompleteDeathSequence();
        }
    }

    private void CompleteDeathSequence()
    {
        if (_deathSequenceFinished)
        {
            return;
        }

        _deathSequenceFinished = true;
        Godot.Collections.Array<Node> gameOverScreens = GetTree().GetNodesInGroup("GameOverUI");
        if (gameOverScreens.Count > 0
            && gameOverScreens[0] is GameOverScreen gameOverScreen)
        {
            gameOverScreen.ShowVictory();
        }
        else
        {
            GD.PushError("No GameOverScreen was found after the boss was defeated.");
        }

        QueueFree();
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
        _usingFallbackHitbox = true;
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
        hitbox.Position = new Vector2(AttackRange * 0.5f, -10.0f);
        return hitbox;
    }
}
