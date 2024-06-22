using Gw2Gizmos.Gw2Api.Contract.WizardsVault;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.WizardsVault;

public interface IWizardsVaultClient : IBlobClient<WizardsVaultSeason>
{
    IWizardsVaultListingsClient Listings { get; }
    IWizardsVaultObjectivesClient Objectives { get; }
}

public interface IWizardsVaultListingsClient
    : IAllExpandableClient<WizardsVaultListing>,
        IBulkExpandableClient<WizardsVaultListing, int>,
        IPaginatedClient<WizardsVaultListing>;

public interface IWizardsVaultObjectivesClient
    : IAllExpandableClient<WizardsVaultObjective>,
        IBulkExpandableClient<WizardsVaultObjective, int>,
        IPaginatedClient<WizardsVaultObjective>;
