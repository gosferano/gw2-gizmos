using Gw2Gizmos.Gw2Api.Contract.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwUpgradesClient
    : IAllExpandableClient<WvwUpgrade>,
        IBulkExpandableClient<WvwUpgrade, int>,
        IPaginatedClient<WvwUpgrade>;
