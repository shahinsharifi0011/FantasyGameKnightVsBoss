using Godot;

public partial class WalkState : KnightState
{
    public override void Enter()
    {
        Controller.PlayAnimation("walk");
    }

    public override void Update(double delta)
    {
        if (HandleLocomotionInput())
        {
            return;
        }

        if (Mathf.IsZeroApprox(Controller.MoveInput))
        {
            TransitionTo(StateMachine.GetState<IdleState>());
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        Controller.SetHorizontalMovement(Controller.MoveInput);
    }
}