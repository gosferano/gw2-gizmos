namespace Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

public sealed class WizardsVaultSeason
{
    public string Title { get; set; } = null!;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int[] Listings { get; set; } = Array.Empty<int>();
    public int[] Objectives { get; set; } = Array.Empty<int>();
}
