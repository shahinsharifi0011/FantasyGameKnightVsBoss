using System;
using Godot;

public partial class KnightStateMachine : Node
{
    [Export]
    public NodePath InitialStatePath { get; set; } = new();

    public KnightState? CurrentState { get; private set; }

    public override void _Ready()
    {
        if (InitialStatePath.IsEmpty)
        {
            throw new InvalidOperationException(
                $"{nameof(InitialStatePath)} must point to a KnightState child.");
        }

        KnightState initialState = GetNodeOrNull<KnightState>(InitialStatePath)
                                   ?? throw new InvalidOperationException(
                                       $"No KnightState was found at '{InitialStatePath}'.");

        TransitionTo(initialState);
    }

    public override void _Process(double delta)
    {
        CurrentState?.Update(delta);
    }

    public void PhysicsUpdate(double delta)
    {
        CurrentState?.PhysicsUpdate(delta);
    }

    public TState GetState<TState>() where TState : KnightState
    {
        foreach (Node child in GetChildren())
        {
            if (child is TState state)
            {
                return state;
            }
        }

        throw new InvalidOperationException(
            $"No state of type {typeof(TState).Name} is a child of this state machine.");
    }

    public void TransitionTo(KnightState nextState)
    {
        if (nextState is null)
        {
            throw new ArgumentNullException(nameof(nextState));
        }

        if (nextState.GetParent() != this)
        {
            throw new ArgumentException(
                "A state must be a direct child of this state machine.",
                nameof(nextState));
        }

        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }
}