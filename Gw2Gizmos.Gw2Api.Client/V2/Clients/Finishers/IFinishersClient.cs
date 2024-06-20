using Gw2Gizmos.Gw2Api.Contract.Finishers;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Finishers;

public interface IFinishersClient
    : IAllExpandableClient<Finisher>,
        IBulkExpandableClient<Finisher, int>,
        IPaginatedClient<Finisher>;
