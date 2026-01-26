using Godot;
using System.Collections.Generic;

public partial class MetaUpgradeMenu : Control
{
    public static MetaUpgradeMenu Instance;

    private Dictionary<MetaResource, int> _resources = new();

    private Dictionary<MetaResource, Label> _resourceLabels = new();

    public override void _Ready()
    {
        Instance = this;

        // TEST VALUES
        foreach (MetaResource res in System.Enum.GetValues(typeof(MetaResource)))
            _resources[res] = 100;

        _resourceLabels[MetaResource.Wood] =
            GetNode<Label>("MarginContainer/VBoxContainer/ResourcesBar/Resource_Wood/AmountLabel");
        _resourceLabels[MetaResource.Meat] =
            GetNode<Label>("MarginContainer/VBoxContainer/ResourcesBar/Resource_Meat/AmountLabel");
        _resourceLabels[MetaResource.Swords] =
            GetNode<Label>("MarginContainer/VBoxContainer/ResourcesBar/Resource_Swords/AmountLabel");
        _resourceLabels[MetaResource.Shields] =
            GetNode<Label>("MarginContainer/VBoxContainer/ResourcesBar/Resource_Shields/AmountLabel");

        UpdateResourceUI();
    }

    public bool CanAfford(MetaResource resource, int cost)
    {
        return _resources[resource] >= cost;
    }

    public void Spend(MetaResource resource, int cost)
    {
        _resources[resource] -= cost;
        UpdateResourceUI();
    }

    private void UpdateResourceUI()
    {
        foreach (var pair in _resourceLabels)
            pair.Value.Text = _resources[pair.Key].ToString();
    }
}
