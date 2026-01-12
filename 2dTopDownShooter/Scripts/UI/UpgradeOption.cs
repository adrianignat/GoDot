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
	// Stored upgrade data (GENERIC)
	// -------------------------------------------------
	private BaseUpgradeResource _upgrade;

	public BaseUpgradeResource GetUpgrade() => _upgrade;

	// -------------------------------------------------
	// Hover animation state
	// -------------------------------------------------
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
		FocusEntered += OnFocusEntered;
		FocusExited += OnFocusExited;
		GuiInput += OnGuiInput;
	}

	// -------------------------------------------------
	// Public setup API (GENERIC)
	// -------------------------------------------------
	public void Setup(BaseUpgradeResource upgrade)
	{
		_upgrade = upgrade;

		HeaderLabel.Text = upgrade.UpgradeName;
		DescriptionLabel.Text =
			$"{upgrade.PercentageIncrease:F1}% increase {upgrade.UpgradeName}";

		ApplyHeaderTexture(upgrade.Quality);
	}

	// -------------------------------------------------
	// Header texture selection
	// -------------------------------------------------
	private void ApplyHeaderTexture(BaseUpgradeResource.UpgradeQuality quality)
	{
		HeaderBG.Texture = quality switch
		{
			BaseUpgradeResource.UpgradeQuality.Common => CommonHeader,
			BaseUpgradeResource.UpgradeQuality.Rare => RareHeader,
			BaseUpgradeResource.UpgradeQuality.Epic => EpicHeader,
			_ => CommonHeader
		};
	}

	// -------------------------------------------------
	// Tween-based hover/focus scale
	// -------------------------------------------------
	private void OnMouseEntered() => ApplyHighlight();
	private void OnMouseExited() => RemoveHighlight();
	private void OnFocusEntered() => ApplyHighlight();
	private void OnFocusExited() => RemoveHighlight();

	private void ApplyHighlight()
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

	private void RemoveHighlight()
	{
		// Don't remove highlight if still focused or mouse is over
		if (HasFocus()) return;

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
	// Input handling (mouse click and keyboard)
	// -------------------------------------------------
	private void OnGuiInput(InputEvent @event)
	{
		// Mouse click
		if (@event is InputEventMouseButton mb &&
			mb.Pressed &&
			mb.ButtonIndex == MouseButton.Left)
		{
			EmitSignal(SignalName.Pressed);
			return;
		}

		// Keyboard selection (Enter, Space, or ui_accept)
		if (@event.IsActionPressed("ui_accept"))
		{
			EmitSignal(SignalName.Pressed);
		}
	}

	[Signal]
	public delegate void PressedEventHandler();
}
