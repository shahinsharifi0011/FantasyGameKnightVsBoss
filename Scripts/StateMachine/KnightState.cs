using Godot;

public abstract partial class KnightState : Node
{
    protected KnightStateMachine StateMachine => GetParent<KnightStateMachine>();
    protected KnightController Controller => StateMachine.GetParent<KnightController>();

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Update(double delta)
    {
    }

    public virtual void PhysicsUpdate(double delta)
    {
    }

    protected void TransitionTo(KnightState nextState)
    {
        StateMachine.TransitionTo(nextState);
    }

    protected bool HandleLocomotionInput()
    {
        if (Input.IsActionPressed(KnightController.BlockAction))
        {
            TransitionTo(StateMachine.GetState<BlockState>());
            return true;
        }

        if (Input.IsActionJustPressed(KnightController.AttackAction))
        {
            TransitionTo(StateMachine.GetState<AttackState>());
            return true;
        }

        if (Input.IsActionJustPressed(KnightController.JumpAction))
        {
            if (Controller.TryJump())
            {
                TransitionTo(StateMachine.GetState<JumpState>());
                return true;
            }
        }

        return false;
    }

    protected void TransitionToLocomotion()
    {
        KnightState nextState = Mathf.IsZeroApprox(Controller.MoveInput)
            ? StateMachine.GetState<IdleState>()
            : StateMachine.GetState<WalkState>();

        TransitionTo(nextState);
    }
}