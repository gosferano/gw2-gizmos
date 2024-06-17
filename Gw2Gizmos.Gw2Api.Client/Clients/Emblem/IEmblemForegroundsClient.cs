namespace Gw2Gizmos.Gw2Api.Client.Clients.Emblem;

public interface IEmblemForegroundsClient
    : IAllExpandableClient<Contract.Emblem.Emblem>,
        IBulkExpandableClient<Contract.Emblem.Emblem, int>,
        IPaginatedClient<Contract.Emblem.Emblem>;
