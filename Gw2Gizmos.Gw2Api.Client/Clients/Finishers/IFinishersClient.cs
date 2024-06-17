using Gw2Gizmos.Gw2Api.Contract.Finishers;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Finishers;

public interface IFinishersClient
    : IAllExpandableClient<Finisher>,
        IBulkExpandableClient<Finisher, int>,
        IPaginatedClient<Finisher>;
