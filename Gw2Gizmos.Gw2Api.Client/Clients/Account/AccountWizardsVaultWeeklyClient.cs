using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountWizardsVaultWeeklyClient
    : BaseBlobClient<AccountWizardsVaultCategory>,
        IAccountWizardsVaultWeeklyClient
{
    internal AccountWizardsVaultWeeklyClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/wizardsvault/weekly";
}
