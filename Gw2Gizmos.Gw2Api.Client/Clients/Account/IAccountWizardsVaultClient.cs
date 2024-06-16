namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountWizardsVaultClient
{
    public IAccountWizardsVaultDailyClient Daily { get; }
    public IAccountWizardsVaultListingsClient Listings { get; }
    public IAccountWizardsVaultSpecialClient Special { get; }
    public IAccountWizardsVaultWeeklyClient Weekly { get; }
}
