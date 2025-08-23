using Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.WizardsVault;

public class WizardsVaultClient : BaseBlobClient<WizardsVaultSeason>, IWizardsVaultClient
{
    internal WizardsVaultClient(HttpClient httpClient)
        : base(httpClient)
    {
        Listings = new WizardsVaultListingsClient(httpClient);
        Objectives = new WizardsVaultObjectivesClient(httpClient);
    }

    protected override string UriPath => "/v2/wizardsvault";

    public IWizardsVaultListingsClient Listings { get; }
    public IWizardsVaultObjectivesClient Objectives { get; }
}

internal class WizardsVaultListingsClient : BaseBulkAllClient<WizardsVaultListing, int>, IWizardsVaultListingsClient
{
    internal WizardsVaultListingsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wizardsvault/listings";
}

internal class WizardsVaultObjectivesClient
    : BaseBulkAllClient<WizardsVaultObjective, int>,
        IWizardsVaultObjectivesClient
{
    internal WizardsVaultObjectivesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wizardsvault/objectives";
}
