using System;
using Godot;

public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maximumHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth { get; set; } = 5;

    [Export]
    public double InvulnerabilityDuration { get; set; } = 0.75;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private double _invulnerabilityRemaining;

    public override void _Ready()
    {
        if (MaxHealth <= 0)
        {
            throw new InvalidOperationException($"{nameof(MaxHealth)} must be greater than zero.");
        }

        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public override void _Process(double delta)
    {
        _invulnerabilityRemaining = Math.Max(0.0, _invulnerabilityRemaining - delta);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage must be greater than zero.");
        }

        if (IsDead || _invulnerabilityRemaining > 0.0)
        {
            return false;
        }

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        _invulnerabilityRemaining = Math.Max(0.0, InvulnerabilityDuration);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth == 0)
        {
            IsDead = true;
            EmitSignal(SignalName.Died);
        }

        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Healing must be greater than zero.");
        }

        if (IsDead)
        {
            return;
        }

        int healedHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        if (healedHealth == CurrentHealth)
        {
            return;
        }

        CurrentHealth = healedHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
