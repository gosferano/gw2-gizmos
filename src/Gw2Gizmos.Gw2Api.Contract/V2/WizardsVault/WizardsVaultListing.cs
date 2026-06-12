namespace Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

public sealed class WizardsVaultListing
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }
    public WizardsVaultListingType Type { get; set; }
    public int Cost { get; set; }
}
