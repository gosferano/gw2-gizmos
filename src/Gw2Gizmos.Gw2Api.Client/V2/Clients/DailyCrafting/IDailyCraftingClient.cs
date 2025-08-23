namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.DailyCrafting;

public interface IDailyCraftingClient
    : IAllExpandableClient<Contract.V2.DailyCrafting.DailyCrafting>,
        IBulkExpandableClient<Contract.V2.DailyCrafting.DailyCrafting, string>,
        IPaginatedClient<Contract.V2.DailyCrafting.DailyCrafting>;
