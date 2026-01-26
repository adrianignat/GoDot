using Godot;

public partial class MetaUpgradeButton : Button
{
    // -------------------------
    // UPGRADE DATA
    // -------------------------
    [Export] public string UpgradeName;
    [Export] public MetaResource ResourceType;
    [Export] public int BaseCost = 10;

    private int _level = 0;

    // -------------------------
    // VISUALS
    // -------------------------
    [Export] public Texture2D NormalTexture;
    [Export] public Texture2D HoverTexture;
    [Export] public Texture2D PressedTexture;

    private TextureRect _background;
    private Label _nameLabel;
    private Label _costLabel;
    private Label _bonusLabel;

    public override void _Ready()
    {
        _background = GetNode<TextureRect>("Background");
        _nameLabel = GetNode<Label>("Content/NameMargin/NameLabel");
        _costLabel = GetNode<Label>("Content/CostMargin/CostLabel");
        _bonusLabel = GetNode<Label>("Content/BonusMargin/BonusLabel");

        // Initial visuals
        _background.Texture = NormalTexture;
        UpdateUI();

        // Button state signals
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        ButtonDown += OnButtonDown;
        ButtonUp += OnButtonUp;
        Pressed += OnPressed;
    }

    // -------------------------
    // COST & UPGRADE LOGIC
    // -------------------------
    public int GetCurrentCost()
    {
        return BaseCost + _level;
    }

    private void TryUpgrade()
    {
        if (!MetaUpgradeMenu.Instance.CanAfford(ResourceType, GetCurrentCost()))
            return;

        MetaUpgradeMenu.Instance.Spend(ResourceType, GetCurrentCost());
        _level++;
        UpdateUI();
    }

    // -------------------------
    // UI UPDATE
    // -------------------------
    private void UpdateUI()
    {
        _nameLabel.Text = UpgradeName;
        _costLabel.Text = $"Cost: {GetCurrentCost()}";
        _bonusLabel.Text = $"Current: {_level * 10} → Next: {(_level + 1) * 10}";
    }

    // -------------------------
    // BUTTON VISUAL STATES
    // -------------------------
    private void OnMouseEntered()
    {
        if (Disabled) return;
        _background.Texture = HoverTexture;
    }

    private void OnMouseExited()
    {
        if (Disabled) return;
        _background.Texture = NormalTexture;
    }

    private void OnButtonDown()
    {
        if (Disabled) return;
        _background.Texture = PressedTexture;
    }

    private void OnButtonUp()
    {
        if (Disabled) return;
        _background.Texture = IsHovered() ? HoverTexture : NormalTexture;
    }

    private void OnPressed()
    {
        TryUpgrade();
    }
}
