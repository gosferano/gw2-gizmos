namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;

public interface IEmblemBackgroundsClient
    : IAllExpandableClient<Contract.Emblem.Emblem>,
        IBulkExpandableClient<Contract.Emblem.Emblem, int>,
        IPaginatedClient<Contract.Emblem.Emblem>;
