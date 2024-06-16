namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountWizardsVaultClient : BaseClient, IAccountWizardsVaultClient
{
    internal AccountWizardsVaultClient(HttpClient httpClient)
        : base(httpClient)
    {
        Daily = new AccountWizardsVaultDailyClient(httpClient);
        Listings = new AccountWizardsVaultListingsClient(httpClient);
        Special = new AccountWizardsVaultSpecialClient(httpClient);
        Weekly = new AccountWizardsVaultWeeklyClient(httpClient);
    }

    protected override string UriPath => "/v2/account/wizardsvault";

    public IAccountWizardsVaultDailyClient Daily { get; }
    public IAccountWizardsVaultListingsClient Listings { get; }
    public IAccountWizardsVaultSpecialClient Special { get; }
    public IAccountWizardsVaultWeeklyClient Weekly { get; }
}
