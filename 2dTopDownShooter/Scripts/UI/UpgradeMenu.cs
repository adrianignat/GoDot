using Godot;

public partial class UpgradeMenu : CanvasLayer
{
    // -------------------------------------------------
    // Exported scenes
    // -------------------------------------------------
    [Export]
    private PackedScene UpgradeOptionScene;

    // -------------------------------------------------
    // Node references
    // -------------------------------------------------
    private Button _showUpgradesButton;
    private HBoxContainer _optionsContainer;

    // -------------------------------------------------
    // RNG (single instance)
    // -------------------------------------------------
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    // -------------------------------------------------
    // Godot lifecycle
    // -------------------------------------------------
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;

        GD.Print("UpgradeMenu ready");

        _rng.Randomize();

        _showUpgradesButton = GetNode<Button>(
            "Root/MenuContainer/VBox/ShowUpgradesButton"
        );

        _optionsContainer = GetNode<HBoxContainer>(
            "Root/MenuContainer/VBox/OptionsContainer"
        );

        _showUpgradesButton.Pressed += OnShowUpgradesPressed;
    }

    // -------------------------------------------------
    // Button handler
    // -------------------------------------------------
    private void OnShowUpgradesPressed()
    {
        GD.Print("Button pressed");

        if (UpgradeOptionScene == null)
        {
            GD.PushError("UpgradeOptionScene not assigned!");
            return;
        }

        ClearOptions();

        for (int i = 0; i < 3; i++)
        {
            UpgradeOption option =
                UpgradeOptionScene.Instantiate<UpgradeOption>();

            _optionsContainer.AddChild(option);
            option.CustomMinimumSize = new Vector2(220, 300);

            // Roll quality using weighted probabilities
            UpgradeOption.UpgradeQuality quality = RollQuality();

            // Example placeholder text (you can replace this later)
            option.Setup(
                quality,
                quality.ToString(),
                "Upgrade description goes here"
            );

            if (i == 0)
                option.GrabFocus();
        }
    }

    // -------------------------------------------------
    // Weighted quality roll
    // -------------------------------------------------
    private UpgradeOption.UpgradeQuality RollQuality()
    {
        // Roll between 0.0 and 1.0
        float roll = _rng.Randf();

        // 70% Common
        if (roll < 0.70f)
            return UpgradeOption.UpgradeQuality.Common;

        // 20% Rare (0.70 → 0.90)
        if (roll < 0.90f)
            return UpgradeOption.UpgradeQuality.Rare;

        // 10% Epic (0.90 → 1.00)
        return UpgradeOption.UpgradeQuality.Epic;
    }

    // -------------------------------------------------
    // Cleanup
    // -------------------------------------------------
    private void ClearOptions()
    {
        foreach (Node child in _optionsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
