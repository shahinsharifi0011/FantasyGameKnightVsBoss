using Godot;

public partial class GameOverScreen : CanvasLayer
{
    private Control _panel = null!;
    private Label _resultLabel = null!;
    private Button _restartButton = null!;
    private bool _isShown;

    public override void _Ready()
    {
        AddToGroup("GameOverUI");
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _resultLabel = GetNode<Label>("Panel/VBoxContainer/GameOverLabel");
        _restartButton = GetNode<Button>("Panel/VBoxContainer/RestartButton");
        _restartButton.Pressed += OnRestartPressed;
        _panel.Hide();
    }

    public void ShowGameOver()
    {
        if (_isShown)
        {
            return;
        }

        _isShown = true;
        _panel.Show();
        GetTree().Paused = true;
    }

    public void ShowVictory()
    {
        _resultLabel.Text = "YOU WIN!";
        ShowGameOver();
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
