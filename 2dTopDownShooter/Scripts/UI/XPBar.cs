using Godot;

public partial class XPBar : Control
{
    [Export] public int XPToNextLevel = 100;
    [Export] public bool EnableTestButtons = true;

    private int _currentXP;

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
        SetXP(0);

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
    // XP Logic
    // =====================

    public void SetXP(int value)
    {
        _currentXP = Mathf.Clamp(value, 0, XPToNextLevel);
        UpdateBar();
    }

    public void AddXP(int amount)
    {
        SetXP(_currentXP + amount);
    }

    private void UpdateBar()
    {
        float percent = (float)_currentXP / XPToNextLevel;

        _fill.Size = new Vector2(
            _maxFillWidth * percent,
            _fill.Size.Y
        );

        int percentageInt = Mathf.RoundToInt(percent * 100f);
        _valueLabel.Text = $"{percentageInt}%";
    }

    // =====================
    // Test Buttons
    // =====================

    private void SetupTestButtons()
    {
        var minusButton = GetNode<Button>("TestControls/MinusButton");
        var plusButton  = GetNode<Button>("TestControls/PlusButton");

        minusButton.Pressed += () => AddXP(-10);
        plusButton.Pressed  += () => AddXP(+10);
    }
}
