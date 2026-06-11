namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwUpgrade
{
    public int Id { get; set; }
    public WvwUpgradeTier[] Tiers { get; set; } = [];
}
