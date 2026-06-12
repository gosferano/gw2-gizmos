namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class AccountWizardsVaultListing
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }
    public AccountWizardsVaultListingType Type { get; set; }
    public int Cost { get; set; }
    public int? Purchased { get; set; }
    public int? PurchaseLimit { get; set; }
}
