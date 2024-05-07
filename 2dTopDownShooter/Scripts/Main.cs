using Godot;

public partial class Main : Node2D
{
    private short _score = 0;
    private Label _scoreLabel;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>("ScoreLabel");
    }

    public void UpdateScore()
    {
        _score += 1;
        _scoreLabel.Text = "Score: " + _score;
    }
}
