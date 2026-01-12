using dTopDownShooter.Scripts;
using Godot;

public partial class XPBar : Control
{
    [Export] public ushort FirstUpgrade = GameConstants.FirstUpgradeThreshold;
    [Export] public ushort UpgradeStep = GameConstants.UpgradeStepIncrement;
    [Export] public bool EnableTestButtons = false;

    private ushort _gold;
    private ushort _goldRequiredForNextUpgrade;
    private ushort _goldAtLastUpgrade;
    private ushort _currentUpgradeStep;

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

        // Initialize upgrade thresholds
        _currentUpgradeStep = FirstUpgrade;
        _goldRequiredForNextUpgrade = FirstUpgrade;
        _goldAtLastUpgrade = 0;

        // Connect to game signals
        Game.Instance.GoldAcquired += OnGoldAcquired;
        Game.Instance.UpgradeSelected += OnUpgradeSelected;

        CallDeferred(nameof(InitializeBar));
    }

    private void OnGoldAcquired(ushort amount)
    {
        _gold += amount;
        UpdateBar();
    }

    private void OnUpgradeSelected(BaseUpgradeResource upgrade)
    {
        ResetBarAfterUpgrade();
    }

    private void ResetBarAfterUpgrade()
    {
        // After upgrade is selected, update thresholds
        _goldAtLastUpgrade = _goldRequiredForNextUpgrade;
        _currentUpgradeStep += UpgradeStep;
        _goldRequiredForNextUpgrade = (ushort)(_goldAtLastUpgrade + _currentUpgradeStep);

        UpdateBar();
    }

    private void InitializeBar()
    {
        RecalculateFill();
        UpdateBar();

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

    private void UpdateBar()
    {
        // Calculate progress within current upgrade tier
        ushort goldInCurrentTier = (ushort)(_gold - _goldAtLastUpgrade);
        ushort goldNeededForTier = (ushort)(_goldRequiredForNextUpgrade - _goldAtLastUpgrade);

        float percent = goldNeededForTier > 0 ? (float)goldInCurrentTier / goldNeededForTier : 0f;
        percent = Mathf.Clamp(percent, 0f, 1f);

        _fill.Size = new Vector2(
            _maxFillWidth * percent,
            _fill.Size.Y
        );

        _valueLabel.Text = $"{goldInCurrentTier}/{goldNeededForTier}";
    }

    // =====================
    // Test Buttons
    // =====================

    private void SetupTestButtons()
    {
        var minusButton = GetNode<Button>("TestControls/MinusButton");
        var plusButton  = GetNode<Button>("TestControls/PlusButton");

        minusButton.Pressed += () => { _gold = (ushort)Mathf.Max(0, _gold - 10); UpdateBar(); };
        plusButton.Pressed  += () => { _gold += 10; UpdateBar(); };
    }
}
