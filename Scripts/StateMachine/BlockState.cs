using Godot;

public partial class BlockState : KnightState
{
    public override void Enter()
    {
        Controller.PlayAnimation("block");
    }

    public override void Update(double delta)
    {
        if (!Input.IsActionPressed(KnightController.BlockAction))
        {
            TransitionToLocomotion();
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        Controller.StopHorizontalMovement();
    }
}