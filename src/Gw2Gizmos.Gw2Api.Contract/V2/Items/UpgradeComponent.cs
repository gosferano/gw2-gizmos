namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class UpgradeComponent : Item
{
    public UpgradeComponentDetails Details { get; set; }
}

public class UpgradeComponentDetails
{
    public UpgradeComponentType Type { get; set; }
    public UpgradeComponentFlag[] Flags { get; set; } = Array.Empty<UpgradeComponentFlag>();
    public InfusionSlotFlag[] InfusionUpgradeFlags { get; set; } = Array.Empty<InfusionSlotFlag>();
    public string Suffix { get; set; }
    public InfixUpgrade InfixUpgrade { get; set; }
    public string[]? Bonuses { get; set; } = Array.Empty<string>();
}
