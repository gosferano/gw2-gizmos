namespace Gw2Gizmos.Gw2Api.Contract.WizardsVault;

public class WizardsVaultSeason
{
    public string Title { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int[] Listings { get; set; } = Array.Empty<int>();
    public int[] Objectives { get; set; } = Array.Empty<int>();
}
