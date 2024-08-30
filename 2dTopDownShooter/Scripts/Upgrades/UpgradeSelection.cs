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

		public UpgradeSelection()
		{
			Game.Instance.UpgradeReady += ShowUpgradeSelectionScene;
			Game.Instance.GoldAcquired += OnGoldAcquired;
			_goldRequiredToUpdate = FirstUpgrade;
		}

		private void OnGoldAcquired(ushort amount)
		{
			_gold += amount;

			if (_gold >= _goldRequiredToUpdate)
			{
				_goldRequiredToUpdate += (ushort)(_gold + UpgradeStep);
				Game.Instance.EmitSignal(Game.SignalName.UpgradeReady);
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
				var chance = rng.RandiRange(0, 100);
				if (chance <= LegendaryUpgradeChance)
				{
					upgrades[i] = new Upgrade(UpgradeType.Health, RarityType.Legendary);
					scene = legendaryUpgradeScene.Instantiate(); ;
				}
				else if (chance <= EpicUpgradeChance)
				{
					upgrades[i] = new Upgrade(UpgradeType.Health, RarityType.Epic);
					scene = epicUpgradeScene.Instantiate();
				}
				else
				{
					upgrades[i] = new Upgrade(UpgradeType.Health, RarityType.Common);
					scene = normalUpgradeScene.Instantiate();
				}

				// Wait until the node is actually freed
				//await ToSignal(GetTree(), "idle_frame");

				options[i].CallDeferred("add_child", scene);
			}

			Show();
		}
	}
}
