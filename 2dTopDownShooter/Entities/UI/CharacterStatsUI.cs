using Godot;
using System.Collections.Generic;

public partial class CharacterStatsUI : Control
{
    private Dictionary<string, Label> _statLabels;

    public override void _Ready()
    {
        _statLabels = new Dictionary<string, Label>
        {
            { "Health", GetNode<Label>("AnchorBL/PanelRoot/Content/VBox/StatsRow_Health/Value") },
            { "Damage", GetNode<Label>("AnchorBL/PanelRoot/Content/VBox/StatsRow_Damage/Value") },
            { "CritChance", GetNode<Label>("AnchorBL/PanelRoot/Content/VBox/StatsRow_CritChance/Value") },
            { "CritDamage", GetNode<Label>("AnchorBL/PanelRoot/Content/VBox/StatsRow_CritDamage/Value") },
        };
    }

    public void SetStats(
        int health,
        int damage,
        float critChance,
        float critDamage
    )
    {
        _statLabels["Health"].Text = health.ToString();
        _statLabels["Damage"].Text = damage.ToString();
        _statLabels["CritChance"].Text = $"{critChance * 100f:0}%";
        _statLabels["CritDamage"].Text = $"{critDamage * 100f:0}%";
    }
}
