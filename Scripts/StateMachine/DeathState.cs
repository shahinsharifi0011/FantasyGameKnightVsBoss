public partial class DeathState : KnightState
{
    public override void Enter()
    {
        Controller.BeginDeath();
    }
}
