using Godot;

namespace dTopDownShooter.Scripts.Upgrades
{
	public partial class UpgradeSelection : Control
	{
		[Export]
		public ushort FirstUpgrade = 5;

		[Export]
		public ushort UpgradeStep = 1;

		[Export]
		private ushort LegendaryUpgradeChance = 5;

		[Export]
		private ushort EpicUpgradeChance = 15;

		private Upgrade[] upgrades = new Upgrade[3];

		private Button[] options;

		private PackedScene normalUpgradeScene = GD.Load<PackedScene>("res://Entities/upgrade_normal.tscn");
		private PackedScene epicUpgradeScene = GD.Load<PackedScene>("res://Entities/upgrade_epic.tscn");
		private PackedScene legendaryUpgradeScene = GD.Load<PackedScene>("res://Entities/upgrade_legendary.tscn");

		private ushort _gold;
		private ushort _goldRequiredToUpdate;
		private ushort _currentUpgradeStep;

		public UpgradeSelection()
		{
			Game.Instance.UpgradeReady += ShowUpgradeSelectionScene;
			Game.Instance.GoldAcquired += OnGoldAcquired;
			_currentUpgradeStep = FirstUpgrade;
			_goldRequiredToUpdate = 1;
		}

		private void OnGoldAcquired(ushort amount)
		{
			_gold += amount;

			if (_gold >= _goldRequiredToUpdate)
			{
				GD.Print($"Gold: {_gold}, Required: {_goldRequiredToUpdate}, CurrentStep: {_currentUpgradeStep}");
				
				Game.Instance.EmitSignal(Game.SignalName.UpgradeReady);

				_currentUpgradeStep += UpgradeStep;
				//_goldRequiredToUpdate += _currentUpgradeStep;
			}
		}

		public override void _Ready()
		{
			Hide();

			var hbox = GetNode<HBoxContainer>("HBoxContainer");

			options = new Button[3];
			options[0] = hbox.GetNode<Button>("Option1");
			options[1] = hbox.GetNode<Button>("Option2");
			options[2] = hbox.GetNode<Button>("Option3");

			options[0].Pressed += () => OnUpgradeButtonPressed(0);
			options[1].Pressed += () => OnUpgradeButtonPressed(1);
			options[2].Pressed += () => OnUpgradeButtonPressed(2);
		}

		private void OnUpgradeButtonPressed(int selection)
		{
			Game.Instance.EmitSignal(Game.SignalName.UpgradeSelected, upgrades[selection]);

			Hide(); // Hide the menu after selection
			Game.Instance.IsPaused = false;
		}

		private void ShowUpgradeSelectionScene()
		{
			Game.Instance.IsPaused = true;

			// Get the bow to check which arrow upgrade is already selected
			var bow = Game.Instance.Player.GetNode<Bow>("Bow");
			var selectedArrowUpgrade = bow.SelectedArrowUpgrade;

			for (int i = 0; i < upgrades.Length; i++)
			{
				var child = options[i].GetChild(0);
				if (child != null)
				{
					options[i].RemoveChild(child);
					child.QueueFree();
				}

				Node scene;
				RandomNumberGenerator rng = new();
				var chance = LegendaryUpgradeChance;
				UpgradeType upgradeType;

				if (chance <= LegendaryUpgradeChance)
				{
					// Legendary can be Bouncing, Piercing, or any other type
					upgradeType = GetLegendaryUpgradeType(rng, selectedArrowUpgrade);
					upgrades[i] = new Upgrade(upgradeType, RarityType.Legendary);
					scene = legendaryUpgradeScene.Instantiate();
				}
				else if (chance <= EpicUpgradeChance)
				{
					// Epic: only Health, WeaponSpeed, Speed
					upgradeType = (UpgradeType)rng.RandiRange(0, 2);
					upgrades[i] = new Upgrade(upgradeType, RarityType.Epic);
					scene = epicUpgradeScene.Instantiate();
				}
				else
				{
					// Common: only Health, WeaponSpeed, Speed
					upgradeType = (UpgradeType)rng.RandiRange(0, 2);
					upgrades[i] = new Upgrade(upgradeType, RarityType.Common);
					scene = normalUpgradeScene.Instantiate();
				}
				var description = scene.GetNode<Label>("Upgrade_Descr");
				description.Text = $"{upgrades[i].Description}";
				options[i].CallDeferred("add_child", scene);
			}

			Show();
		}

		private UpgradeType GetLegendaryUpgradeType(RandomNumberGenerator rng, UpgradeType? selectedArrowUpgrade)
		{
			// Available types: Health(0), WeaponSpeed(1), Speed(2), Bouncing(3), Piercing(4)
			// But Bouncing and Piercing are mutually exclusive

			if (selectedArrowUpgrade == UpgradeType.Bouncing)
			{
				// Exclude Piercing (4), allow 0-3
				return (UpgradeType)rng.RandiRange(0, 3);
			}
			else if (selectedArrowUpgrade == UpgradeType.Piercing)
			{
				// Exclude Bouncing (3), allow 0-2 or 4
				var roll = rng.RandiRange(0, 3);
				if (roll == 3) return UpgradeType.Piercing;
				return (UpgradeType)roll;
			}
			else
			{
				// Neither selected yet, allow all 0-4
				return (UpgradeType)rng.RandiRange(0, 4);
			}
		}
	}
}
