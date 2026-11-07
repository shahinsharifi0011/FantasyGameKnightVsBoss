using Godot;

public partial class GameOverScreen : CanvasLayer
{
    private Control _panel = null!;
    private Button _restartButton = null!;

    public override void _Ready()
    {
        AddToGroup("GameOverUI");
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _restartButton = GetNode<Button>("Panel/VBoxContainer/RestartButton");
        _restartButton.Pressed += OnRestartPressed;
        _panel.Hide();
    }

    public void ShowGameOver()
    {
        _panel.Show();
        GetTree().Paused = true;
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
