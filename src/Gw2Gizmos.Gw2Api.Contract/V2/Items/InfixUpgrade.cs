namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class InfixUpgrade
{
    public int Id { get; set; }
    public InfixUpgradeAttribute[] Attributes { get; set; } = [];
    public InfixUpgradeBuff? Buff { get; set; }
}
