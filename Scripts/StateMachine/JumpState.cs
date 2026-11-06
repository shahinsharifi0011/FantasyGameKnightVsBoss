using Godot;

public partial class JumpState : KnightState
{
    public override void Enter()
    {
        Controller.PlayAnimation("jump");
    }

    public override void Update(double delta)
    {
        if (Controller.IsOnFloor() && Controller.Velocity.Y >= 0.0f)
        {
            TransitionToLocomotion();
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        Controller.SetHorizontalMovement(Controller.MoveInput);
    }
}