using Godot;

public partial class IdleState : KnightState
{
    public override void Enter()
    {
        Controller.PlayAnimation("idle");
    }

    public override void Update(double delta)
    {
        if (HandleLocomotionInput())
        {
            return;
        }

        if (!Mathf.IsZeroApprox(Controller.MoveInput))
        {
            TransitionTo(StateMachine.GetState<WalkState>());
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        Controller.StopHorizontalMovement();
    }
}