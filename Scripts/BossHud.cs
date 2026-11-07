using System;
using Godot;

public partial class BossHud : ProgressBar
{
    private ProgressBar _playerProgressBar = null!;

    public override void _Ready()
    {
        _playerProgressBar = GetParent().GetNode<ProgressBar>("PlayerProgressBar");

        Godot.Collections.Array<Node> players = GetTree().GetNodesInGroup("Player");
        if (players.Count == 0)
        {
            throw new InvalidOperationException("No node was found in the 'Player' group.");
        }

        HealthComponent playerHealth = players[0].GetNodeOrNull<HealthComponent>("HealthComponent")
            ?? throw new InvalidOperationException(
                "The first node in the 'Player' group has no HealthComponent child.");

        playerHealth.HealthChanged += OnPlayerHealthChanged;
        UpdatePlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    public override void _Process(double delta)
    {
        Godot.Collections.Array<Node> bosses = GetTree().GetNodesInGroup("Boss");
        if (bosses.Count == 0)
        {
            return;
        }

        HealthComponent health = bosses[0].GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health is null)
        {
            return;
        }

        MaxValue = health.MaxHealth;
        Value = health.CurrentHealth;
    }

    private void OnPlayerHealthChanged(int currentHealth, int maximumHealth)
    {
        UpdatePlayerHealth(currentHealth, maximumHealth);
    }

    private void UpdatePlayerHealth(int currentHealth, int maximumHealth)
    {
        _playerProgressBar.MaxValue = maximumHealth;
        _playerProgressBar.Value = currentHealth;
    }
}
