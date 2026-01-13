using System.Collections.Generic;
using dTopDownShooter.Scripts;
using Godot;

public partial class UpgradeMenu : Control
{
	// -------------------------------------------------
	// Exported scenes and settings
	// -------------------------------------------------
	[Export]
	private PackedScene UpgradeOptionScene;

	[Export]
	public ushort FirstUpgrade = GameConstants.FirstUpgradeThreshold;

	[Export]
	public ushort UpgradeStep = GameConstants.UpgradeStepIncrement;

	private const int UpgradeOptionCount = GameConstants.UpgradeOptionCount;

	// -------------------------------------------------
	// Node references
	// -------------------------------------------------
	private HBoxContainer _optionsContainer;

	// -------------------------------------------------
	// State tracking
	// -------------------------------------------------
	private ushort _gold;
	private ushort _goldRequiredToUpdate;
	private ushort _currentUpgradeStep;
	private int _pendingUpgrades = 0;
	private bool _isShowingSelection = false;

	// -------------------------------------------------
	// Godot lifecycle
	// -------------------------------------------------
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_currentUpgradeStep = FirstUpgrade;
		_goldRequiredToUpdate = FirstUpgrade;

		_optionsContainer = GetNode<HBoxContainer>("VBoxContainer/OptionsContainer");

		// Connect to game signals
		Game.Instance.UpgradeReady += OnUpgradeReady;
		Game.Instance.GoldAcquired += OnGoldAcquired;
		Game.Instance.PlayerHealthChanged += OnPlayerHealthChanged;

		// Hide menu initially
		Visible = false;
	}

	// -------------------------------------------------
	// Signal handlers
	// -------------------------------------------------
	private void OnGoldAcquired(ushort amount)
	{
		if (Game.Instance.Player.IsDead)
			return;

		_gold += amount;

		if (_gold >= _goldRequiredToUpdate)
		{
			Game.Instance.EmitSignal(Game.SignalName.UpgradeReady);
			_currentUpgradeStep += UpgradeStep;
			_goldRequiredToUpdate += _currentUpgradeStep;
		}
	}

	private void OnUpgradeReady()
	{
		if (Game.Instance.Player.IsDead)
			return;

		if (_isShowingSelection)
		{
			_pendingUpgrades++;
			GD.Print($"Upgrade queued. Pending upgrades: {_pendingUpgrades}");
		}
		else
		{
			ShowSelectionScreen();
		}
	}

	private void OnPlayerHealthChanged(ushort health)
	{
		if (health == 0 && _isShowingSelection)
		{
			Visible = false;
			_isShowingSelection = false;
			Game.Instance.IsUpgradeSelectionShowing = false;
			_pendingUpgrades = 0;
		}
	}

	// -------------------------------------------------
	// Selection screen logic
	// -------------------------------------------------
	private void ShowSelectionScreen()
	{
		if (UpgradeOptionScene == null)
		{
			GD.PushError("UpgradeOptionScene not assigned!");
			return;
		}

		Game.Instance.IsPaused = true;
		_isShowingSelection = true;
		Game.Instance.IsUpgradeSelectionShowing = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		ClearOptions();

		var upgrades = UpgradeFactory.CreateUpgradeRoll(UpgradeOptionCount, Game.Instance.Player.LuckLevel);
		var options = new List<UpgradeOption>();

		for (int i = 0; i < upgrades.Count; i++)
		{
			UpgradeOption option = UpgradeOptionScene.Instantiate<UpgradeOption>();

			_optionsContainer.AddChild(option);
			option.Setup(upgrades[i]);
			option.Pressed += () => OnUpgradeSelected(option.GetUpgrade());
			options.Add(option);
		}

		// Set up focus neighbors for keyboard navigation
		for (int i = 0; i < options.Count; i++)
		{
			var option = options[i];
			var leftNeighbor = options[(i - 1 + options.Count) % options.Count];
			var rightNeighbor = options[(i + 1) % options.Count];

			option.FocusNeighborLeft = leftNeighbor.GetPath();
			option.FocusNeighborRight = rightNeighbor.GetPath();
			// Also set up/down to wrap horizontally
			option.FocusNeighborTop = leftNeighbor.GetPath();
			option.FocusNeighborBottom = rightNeighbor.GetPath();
		}

		options[0].GrabFocus();
		Visible = true;
	}

	private void OnUpgradeSelected(BaseUpgradeResource upgrade)
	{
		Game.Instance.EmitSignal(Game.SignalName.UpgradeSelected, upgrade);

		if (_pendingUpgrades > 0)
		{
			_pendingUpgrades--;
			GD.Print($"Showing next upgrade. Remaining: {_pendingUpgrades}");
			ShowSelectionScreen();
		}
		else
		{
			Visible = false;
			_isShowingSelection = false;
			Game.Instance.IsUpgradeSelectionShowing = false;
			Game.Instance.IsPaused = false;
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
