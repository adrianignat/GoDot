using Godot;
using System.Collections.Generic;

namespace dTopDownShooter.Scripts.Upgrades
{
	public partial class UpgradeSelection : Control
	{
		[Export] public ushort FirstUpgrade = 5;
		[Export] public ushort UpgradeStep = 1;
		[Export] private ushort LegendaryUpgradeChance = 5;
		[Export] private ushort EpicUpgradeChance = 15;

		private const int UpgradeOptionCount = 3;

		private readonly PackedScene _normalScene = GD.Load<PackedScene>("res://Entities/Upgrades/upgrade_normal.tscn");
		private readonly PackedScene _epicScene = GD.Load<PackedScene>("res://Entities/Upgrades/upgrade_epic.tscn");
		private readonly PackedScene _legendaryScene = GD.Load<PackedScene>("res://Entities/Upgrades/upgrade_legendary.tscn");

		private readonly RandomNumberGenerator _rng = new();
		private readonly Upgrade[] _upgrades = new Upgrade[UpgradeOptionCount];
		private Button[] _optionButtons;

		private ushort _gold;
		private ushort _goldRequiredToUpdate;
		private ushort _currentUpgradeStep;
		private int _pendingUpgrades = 0;
		private bool _isShowingSelection = false;

		public override void _Ready()
		{
			// UI must process during pause to handle button clicks
			ProcessMode = ProcessModeEnum.Always;

			_currentUpgradeStep = FirstUpgrade;
			_goldRequiredToUpdate = FirstUpgrade;

			// Connect to signals in _Ready, not constructor, to ensure Game.Instance is valid
			Game.Instance.UpgradeReady += OnUpgradeReady;
			Game.Instance.GoldAcquired += OnGoldAcquired;
			Game.Instance.PlayerHealthChanged += OnPlayerHealthChanged;

			Hide();
			InitializeButtons();
		}

		private void OnPlayerHealthChanged(ushort health)
		{
			// If player dies while upgrade selection is showing, hide it
			if (health == 0 && _isShowingSelection)
			{
				Hide();
				_isShowingSelection = false;
				Game.Instance.IsUpgradeSelectionShowing = false;
				_pendingUpgrades = 0;
				// Note: Don't unpause - player death pauses the game
			}
		}

		private void InitializeButtons()
		{
			var hbox = GetNode<HBoxContainer>("HBoxContainer");
			_optionButtons = new Button[UpgradeOptionCount];

			for (int i = 0; i < UpgradeOptionCount; i++)
			{
				_optionButtons[i] = hbox.GetNode<Button>($"Option{i + 1}");
				int index = i;
				_optionButtons[i].Pressed += () => SelectUpgrade(index);
			}
		}

		private void OnGoldAcquired(ushort amount)
		{
			// Don't process gold if player is dead
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
			// Don't show upgrades if player is dead
			if (Game.Instance.Player.IsDead)
				return;

			if (_isShowingSelection)
			{
				// Already showing selection screen - queue this upgrade
				_pendingUpgrades++;
				GD.Print($"Upgrade queued. Pending upgrades: {_pendingUpgrades}");
			}
			else
			{
				// Not showing - display the selection screen
				ShowSelectionScreen();
			}
		}

		private void SelectUpgrade(int index)
		{
			Game.Instance.EmitSignal(Game.SignalName.UpgradeSelected, _upgrades[index]);

			if (_pendingUpgrades > 0)
			{
				// More upgrades waiting - show next selection
				_pendingUpgrades--;
				GD.Print($"Showing next upgrade. Remaining: {_pendingUpgrades}");
				ShowSelectionScreen();
			}
			else
			{
				// No more upgrades - close and unpause
				Hide();
				_isShowingSelection = false;
				Game.Instance.IsUpgradeSelectionShowing = false;
				Game.Instance.IsPaused = false;
			}
		}

		private void ShowSelectionScreen()
		{
			Game.Instance.IsPaused = true;
			_isShowingSelection = true;
			Game.Instance.IsUpgradeSelectionShowing = true;

			var usedTypes = new HashSet<UpgradeType>();
			var lockedArrowType = GetLockedArrowUpgrade();

			for (int i = 0; i < UpgradeOptionCount; i++)
			{
				ClearButtonContent(_optionButtons[i]);

				var rarity = RollRarity();
				var type = GetAvailableUpgradeType(usedTypes, lockedArrowType, rarity);
				usedTypes.Add(type);

				_upgrades[i] = new Upgrade(type, rarity);
				AttachUpgradeScene(_optionButtons[i], _upgrades[i]);
			}

			Show();
		}

		private static UpgradeType? GetLockedArrowUpgrade()
		{
			var bow = Game.Instance.Player.GetNode<Bow>("Bow");
			return bow.SelectedArrowUpgrade;
		}

		private RarityType RollRarity()
		{
			var luckLevel = Game.Instance.Player.LuckLevel;
			// Each 10 luck adds +2% legendary and +5% epic chance
			var legendaryChance = LegendaryUpgradeChance + (luckLevel / 5);
			var epicChance = EpicUpgradeChance + (luckLevel / 2);

			var roll = _rng.RandiRange(1, 100);

			// Use cumulative thresholds: legendary first, then epic range starts after
			if (roll <= legendaryChance)
				return RarityType.Legendary;
			if (roll <= legendaryChance + epicChance)
				return RarityType.Epic;
			return RarityType.Common;
		}

		private UpgradeType GetAvailableUpgradeType(HashSet<UpgradeType> usedTypes, UpgradeType? lockedArrowType, RarityType rarity)
		{
			var available = GetBaseUpgradeTypes();

			if (rarity == RarityType.Legendary)
				AddArrowUpgrades(available, lockedArrowType);

			// Remove maxed out upgrades
			if (Game.Instance.Player.GetMagnetShape().Radius >= Player.MaxMagnetRadius)
				available.Remove(UpgradeType.Magnet);

			var dynamiteThrower = Game.Instance.Player.GetDynamiteThrower();
			if (dynamiteThrower.BonusBlastRadius >= Player.MaxDynamiteBlastRadius)
				available.Remove(UpgradeType.Dynamite);

			available.RemoveAll(t => usedTypes.Contains(t));

			if (available.Count == 0)
				return UpgradeType.Health;

			return available[_rng.RandiRange(0, available.Count - 1)];
		}

		private List<UpgradeType> GetBaseUpgradeTypes()
		{
			return
			[
				UpgradeType.Health,
				UpgradeType.WeaponSpeed,
				UpgradeType.Speed,
				UpgradeType.Luck,
				UpgradeType.Magnet,
				UpgradeType.Dynamite
			];
		}

		private static void AddArrowUpgrades(List<UpgradeType> types, UpgradeType? lockedArrowType)
		{
			if (lockedArrowType == UpgradeType.Bouncing)
				types.Add(UpgradeType.Bouncing);
			else if (lockedArrowType == UpgradeType.Piercing)
				types.Add(UpgradeType.Piercing);
			else
			{
				types.Add(UpgradeType.Bouncing);
				types.Add(UpgradeType.Piercing);
			}
		}

		private static void ClearButtonContent(Button button)
		{
			if (button.GetChildCount() > 0)
			{
				var child = button.GetChild(0);
				button.RemoveChild(child);
				child.QueueFree();
			}
		}

		private void AttachUpgradeScene(Button button, Upgrade upgrade)
		{
			var scene = GetSceneForRarity(upgrade.Rarity);
			var title = scene.GetNode<Label>("Upgrade_Title");
			var description = scene.GetNode<Label>("Upgrade_Descr");
			title.Text = upgrade.Title;
			description.Text = upgrade.Description;
			button.CallDeferred("add_child", scene);
		}

		private Node GetSceneForRarity(RarityType rarity)
		{
			return rarity switch
			{
				RarityType.Legendary => _legendaryScene.Instantiate(),
				RarityType.Epic => _epicScene.Instantiate(),
				_ => _normalScene.Instantiate()
			};
		}
	}
}
