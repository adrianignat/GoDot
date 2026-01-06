using Godot;
using System;

public partial class MainMenu_2 : Control
{
    [Export]
    public bool TestMode = true;

    private const int PressedTextOffsetY = 10;

    private TextureButton _startButton;
    private TextureButton _easyButton;
    private TextureButton _optionsButton;
    private TextureButton _exitButton;

    public override void _Ready()
    {
        _startButton = GetNode<TextureButton>(
            "CenterContainer/VBoxContainer/StartButton");
        _easyButton = GetNode<TextureButton>(
            "CenterContainer/VBoxContainer/EasyButton");
        _optionsButton = GetNode<TextureButton>(
            "CenterContainer/VBoxContainer/OptionsButton");
        _exitButton = GetNode<TextureButton>(
            "CenterContainer/VBoxContainer/ExitButton");

        SetupButton(_startButton, OnStartPressed);
        SetupButton(_easyButton, OnEasyPressed);
        SetupButton(_optionsButton, OnOptionsPressed);
        SetupButton(_exitButton, OnExitPressed);
    }

    private void SetupButton(TextureButton button, Action onPressed)
    {
        var label = button.GetNode<Label>("Label");

        button.Pressed += () =>
        {
            label.AddThemeConstantOverride(
                "margin_top", PressedTextOffsetY);

            onPressed?.Invoke();
        };

        button.ButtonUp += () =>
        {
            label.AddThemeConstantOverride(
                "margin_top", 0);
        };
    }

    private void OnStartPressed()
    {
        if (TestMode)
        {
            GD.Print("Start pressed (TEST MODE)");
            return;
        }
    }

    private void OnEasyPressed()
    {
        if (TestMode)
        {
            GD.Print("Easy pressed (TEST MODE)");
            return;
        }
    }

    private void OnOptionsPressed()
    {
        if (TestMode)
        {
            GD.Print("Options pressed (TEST MODE)");
            return;
        }
    }

    private void OnExitPressed()
    {
        if (TestMode)
        {
            GD.Print("Exit pressed (TEST MODE)");
            return;
        }

        GetTree().Quit();
    }
}
