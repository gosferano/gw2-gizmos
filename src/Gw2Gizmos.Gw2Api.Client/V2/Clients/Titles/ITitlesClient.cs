using Gw2Gizmos.Gw2Api.Contract.Titles;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Titles;

public interface ITitlesClient
    : IAllExpandableClient<Title>,
        IBulkExpandableClient<Title, int>,
        IPaginatedClient<Title>;
