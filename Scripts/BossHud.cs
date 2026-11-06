using Godot;

public partial class BossHud : CanvasLayer
{
    [Export]
    public NodePath PlayerHealthPath { get; set; } = new("../Player/HealthComponent");

    [Export]
    public NodePath BossHealthPath { get; set; } = new("../Boss/HealthComponent");

    [Export]
    public NodePath PlayerLabelPath { get; set; } = new("PlayerHealth");

    [Export]
    public NodePath BossLabelPath { get; set; } = new("BossHealth");

    [Export]
    public NodePath StatusLabelPath { get; set; } = new("Status");

    private HealthComponent _playerHealth = null!;
    private HealthComponent _bossHealth = null!;
    private Label _playerLabel = null!;
    private Label _bossLabel = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        _playerHealth = GetNode<HealthComponent>(PlayerHealthPath);
        _bossHealth = GetNode<HealthComponent>(BossHealthPath);
        _playerLabel = GetNode<Label>(PlayerLabelPath);
        _bossLabel = GetNode<Label>(BossLabelPath);
        _statusLabel = GetNode<Label>(StatusLabelPath);

        _playerHealth.HealthChanged += OnPlayerHealthChanged;
        _playerHealth.Died += OnPlayerDied;
        _bossHealth.HealthChanged += OnBossHealthChanged;
        _bossHealth.Died += OnBossDied;
        UpdatePlayerHealth(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        UpdateBossHealth(_bossHealth.CurrentHealth, _bossHealth.MaxHealth);
    }

    private void OnPlayerHealthChanged(int currentHealth, int maximumHealth)
    {
        UpdatePlayerHealth(currentHealth, maximumHealth);
    }

    private void OnBossHealthChanged(int currentHealth, int maximumHealth)
    {
        UpdateBossHealth(currentHealth, maximumHealth);
    }

    private void OnPlayerDied()
    {
        _statusLabel.Text = "Knight defeated - restarting...";
    }

    private void OnBossDied()
    {
        _statusLabel.Text = "Boss defeated!";
    }

    private void UpdatePlayerHealth(int currentHealth, int maximumHealth)
    {
        _playerLabel.Text = $"KNIGHT  {currentHealth} / {maximumHealth}";
    }

    private void UpdateBossHealth(int currentHealth, int maximumHealth)
    {
        _bossLabel.Text = $"BOSS  {currentHealth} / {maximumHealth}";
    }
}
