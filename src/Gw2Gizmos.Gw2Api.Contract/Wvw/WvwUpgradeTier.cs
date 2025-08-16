namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwUpgradeTier
{
    public string Name { get; set; }
    public int YaksRequired { get; set; }
    public WvwUpgradeTierUpgrade[] Upgrades { get; set; } = Array.Empty<WvwUpgradeTierUpgrade>();
}
