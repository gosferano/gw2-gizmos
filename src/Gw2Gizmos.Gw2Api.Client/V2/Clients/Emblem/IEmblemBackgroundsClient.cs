namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;

public interface IEmblemBackgroundsClient
    : IAllExpandableClient<Contract.V2.Emblem.Emblem>,
        IBulkExpandableClient<Contract.V2.Emblem.Emblem, int>,
        IPaginatedClient<Contract.V2.Emblem.Emblem>;
