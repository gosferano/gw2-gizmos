namespace Gw2Gizmos.Gw2Api.Contract.WizardsVault;

public class WizardsVaultListing
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }
    public WizardsVaultListingType Type { get; set; }
    public int Cost { get; set; }
}
