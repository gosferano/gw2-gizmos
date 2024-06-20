namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.DailyCrafting;

public interface IDailyCraftingClient
    : IAllExpandableClient<Contract.DailyCrafting.DailyCrafting>,
        IBulkExpandableClient<Contract.DailyCrafting.DailyCrafting, string>,
        IPaginatedClient<Contract.DailyCrafting.DailyCrafting>;
