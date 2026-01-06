using Godot;

public partial class HealthBar : Control
{
    [Export] public int MaxHealth = 100;
    [Export] public bool EnableTestButtons = true;

    private int _currentHealth;

    private NinePatchRect _base;
    private TextureRect _fill;
    private Label _valueLabel;

    private float _leftPadding;
    private float _rightPadding;
    private float _maxFillWidth;

    public override void _Ready()
    {
        _base = GetNode<NinePatchRect>("Base");
        _fill = GetNode<TextureRect>("Base/Fill");
        _valueLabel = GetNode<Label>("Base/ValueLabel");

        _leftPadding = _base.PatchMarginLeft;
        _rightPadding = _base.PatchMarginRight;

        CallDeferred(nameof(InitializeBar));
    }

    private void InitializeBar()
    {
        RecalculateFill();
        SetHealth(MaxHealth);

        _base.Resized += OnBaseResized;

        if (EnableTestButtons)
            SetupTestButtons();
    }

    private void OnBaseResized()
    {
        RecalculateFill();
        UpdateBar();
    }

    private void RecalculateFill()
    {
        float baseWidth = _base.Size.X;
        float baseHeight = _base.Size.Y;

        _maxFillWidth = baseWidth - _leftPadding - _rightPadding;

        _fill.Position = new Vector2(_leftPadding, 0);
        _fill.Size = new Vector2(_maxFillWidth, baseHeight);
    }

    // =====================
    // Health Logic
    // =====================

    public void SetHealth(int value)
    {
        _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
        UpdateBar();
    }

    public void ModifyHealth(int amount)
    {
        SetHealth(_currentHealth + amount);
    }

    private void UpdateBar()
    {
        float percent = (float)_currentHealth / MaxHealth;

        _fill.Size = new Vector2(
            _maxFillWidth * percent,
            _fill.Size.Y
        );

        // 🔢 Update numeric display
        _valueLabel.Text = $"{_currentHealth} / {MaxHealth}";
    }

    // =====================
    // Test Buttons
    // =====================

    private void SetupTestButtons()
    {
        var minusButton = GetNode<Button>("TestControls/MinusButton");
        var plusButton  = GetNode<Button>("TestControls/PlusButton");

        minusButton.Pressed += () => ModifyHealth(-10);
        plusButton.Pressed  += () => ModifyHealth(+10);
    }
}
