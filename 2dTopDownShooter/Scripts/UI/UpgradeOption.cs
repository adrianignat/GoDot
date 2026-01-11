using Godot;

public partial class UpgradeOption : Control
{
    // -------------------------------------------------
    // Exported node references
    // -------------------------------------------------
    [ExportCategory("Nodes")]
    [Export] public TextureRect HeaderBG;
    [Export] public Label HeaderLabel;
    [Export] public TextureRect DescriptionBG;
    [Export] public Label DescriptionLabel;

    // -------------------------------------------------
    // Header background variants
    // -------------------------------------------------
    [ExportCategory("Header Sprites")]
    [Export] public Texture2D CommonHeader;
    [Export] public Texture2D RareHeader;
    [Export] public Texture2D EpicHeader;

    // -------------------------------------------------
    // Quality enum (kept inline as requested)
    // -------------------------------------------------
    public enum UpgradeQuality
    {
        Common,
        Rare,
        Epic
    }

    public UpgradeQuality Quality { get; private set; }

    // -------------------------------------------------
    // Hover animation state
    // -------------------------------------------------
    private Vector2 _basePosition;
    private Vector2 _baseScale;
    private Tween _hoverTween;

    // -------------------------------------------------
    // Godot lifecycle
    // -------------------------------------------------
    public override void _Ready()
    {
        _baseScale = Scale;
        PivotOffset = Size / 2f;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        GuiInput += OnGuiInput;
    }

    // -------------------------------------------------
    // Public setup API
    // -------------------------------------------------
    public void Setup(
        UpgradeQuality quality,
        string headerText,
        string descriptionText
    )
    {
        Quality = quality;
        HeaderLabel.Text = headerText;
        DescriptionLabel.Text = descriptionText;

        ApplyHeaderTexture(quality);
    }

    // -------------------------------------------------
    // Header texture selection
    // -------------------------------------------------
    private void ApplyHeaderTexture(UpgradeQuality quality)
    {
        HeaderBG.Texture = quality switch
        {
            UpgradeQuality.Common => CommonHeader,
            UpgradeQuality.Rare => RareHeader,
            UpgradeQuality.Epic => EpicHeader,
            _ => CommonHeader
        };
    }

    // -------------------------------------------------
    // Tween-based hover lift / highlight
    // -------------------------------------------------
    private void OnMouseEntered()
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();

        _hoverTween.TweenProperty(
            this,
            "scale",
            _baseScale * 1.06f,
            0.15f
        ).SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Back);

        Modulate = new Color(1.1f, 1.1f, 1.1f);
    }

    private void OnMouseExited()
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();

        _hoverTween.TweenProperty(
            this,
            "scale",
            _baseScale,
            0.12f
        ).SetEase(Tween.EaseType.Out);

        Modulate = Colors.White;
    }


    // -------------------------------------------------
    // Click handling
    // -------------------------------------------------
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb &&
            mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.Pressed);
        }
    }

    [Signal]
    public delegate void PressedEventHandler();
}
