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
    // Godot lifecycle
    // -------------------------------------------------
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;

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
        if (UpgradeOptionScene == null)
        {
            GD.PushError("UpgradeOptionScene not assigned!");
            return;
        }

        ClearOptions();

        var upgrades = UpgradeFactory.CreateUpgradeRoll(3);

        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeOption option =
                UpgradeOptionScene.Instantiate<UpgradeOption>();

            _optionsContainer.AddChild(option);
            option.CustomMinimumSize = new Vector2(220, 300);

            option.Setup(upgrades[i]);

            if (i == 0)
                option.GrabFocus();
        }

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
