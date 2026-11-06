using Godot;

public partial class AttackState : KnightState
{
    [Export]
    public float AttackDuration { get; set; } = 0.4f;

    private double _elapsed;

    public override void Enter()
    {
        Controller.PlayAnimation("attack");
        Controller.SetAttackActive(true);
        _elapsed = 0.0;
    }

    public override void Exit()
    {
        Controller.SetAttackActive(false);
    }

    public override void Update(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= AttackDuration)
        {
            TransitionToLocomotion();
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        Controller.StopHorizontalMovement();
    }
}