namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildUpgrade
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public GuildUpgradeType Type { get; set; }
    public string Icon { get; set; } = null!;
    public int BuildTime { get; set; }
    public int RequiredLevel { get; set; }
    public int Experience { get; set; }
    public int[] Prerequisites { get; set; } = Array.Empty<int>();
    public GuildUpgradeCost[] Costs { get; set; } = Array.Empty<GuildUpgradeCost>();
}
