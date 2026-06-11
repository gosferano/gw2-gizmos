namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwUpgradeTier
{
    public string Name { get; set; } = null!;
    public int YaksRequired { get; set; }
    public WvwUpgradeTierUpgrade[] Upgrades { get; set; } = Array.Empty<WvwUpgradeTierUpgrade>();
}
