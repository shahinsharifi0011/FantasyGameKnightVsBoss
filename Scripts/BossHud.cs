using Godot;

public partial class BossHud : ProgressBar
{
    private ProgressBar _playerProgressBar = null!;

    public override void _Ready()
    {
        _playerProgressBar = GetParent().GetNode<ProgressBar>("PlayerProgressBar");
    }

    public override void _Process(double delta)
    {
        UpdateHealthBar("Boss", this);
        UpdateHealthBar("Player", _playerProgressBar);
    }

    private void UpdateHealthBar(string groupName, ProgressBar progressBar)
    {
        Godot.Collections.Array<Node> characters = GetTree().GetNodesInGroup(groupName);
        if (characters.Count == 0)
        {
            return;
        }

        HealthComponent health = characters[0].GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health is null)
        {
            return;
        }

        progressBar.MaxValue = health.MaxHealth;
        progressBar.Value = health.CurrentHealth;
    }
}
