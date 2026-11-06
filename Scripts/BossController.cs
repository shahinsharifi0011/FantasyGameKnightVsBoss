using Godot;

public partial class BossController : CharacterBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; set; } = new("HealthComponent");

    [Export]
    public string PlayerGroupName { get; set; } = "player";

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
    public int AttackDamage { get; set; } = 1;

    private HealthComponent _health = null!;
    private KnightController _player = null!;
    private Polygon2D _body = null!;
    private double _attackCooldownRemaining = 0.6;
    private double _attackWindupRemaining;
    private bool _isAttacking;
    private bool _isDead;

    public override void _Ready()
    {
        AddToGroup("boss");
        _health = GetNode<HealthComponent>(HealthComponentPath);
        _body = GetNode<Polygon2D>("Body");
        _health.Died += OnDied;
        _player = GetTree().GetFirstNodeInGroup(PlayerGroupName) as KnightController;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead || _player is null || !GodotObject.IsInstanceValid(_player))
        {
            return;
        }

        Vector2 toPlayer = _player!.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();

        if (!IsOnFloor())
        {
            Velocity += Vector2.Down * Gravity * (float)delta;
        }
        else if (Velocity.Y > 0.0f)
        {
            Velocity = new Vector2(Velocity.X, 0.0f);
        }

        if (distance > AttackRange)
        {
            Velocity = new Vector2(Mathf.Sign(toPlayer.X) * MoveSpeed, Velocity.Y);
            _body.Scale = new Vector2(toPlayer.X < 0.0f ? -1.0f : 1.0f, 1.0f);
            MoveAndSlide();

            if (_isAttacking)
            {
                _isAttacking = false;
                _body.Color = new Color(0.58f, 0.14f, 0.12f);
            }

            return;
        }

        Velocity = new Vector2(0.0f, Velocity.Y);
        MoveAndSlide();
        _attackCooldownRemaining = System.Math.Max(0.0, _attackCooldownRemaining - delta);

        if (_isAttacking)
        {
            _attackWindupRemaining -= delta;
            if (_attackWindupRemaining <= 0.0)
            {
                _isAttacking = false;
                _body.Color = new Color(0.58f, 0.14f, 0.12f);
                if (_player is not null
                    && GodotObject.IsInstanceValid(_player)
                    && _player.GlobalPosition.DistanceTo(GlobalPosition) <= AttackRange)
                {
                    _player.TakeDamage(AttackDamage);
                }
            }

            return;
        }

        if (_attackCooldownRemaining <= 0.0)
        {
            _isAttacking = true;
            _attackWindupRemaining = AttackWindup;
            _attackCooldownRemaining = AttackCooldown;
            _body.Color = new Color(0.9f, 0.62f, 0.12f);
        }
    }

    private void OnDied()
    {
        _isDead = true;
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        _body.Color = new Color(0.3f, 0.3f, 0.3f);
    }
}
