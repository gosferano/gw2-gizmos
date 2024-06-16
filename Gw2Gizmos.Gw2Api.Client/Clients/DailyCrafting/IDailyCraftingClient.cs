namespace Gw2Gizmos.Gw2Api.Client.Clients.DailyCrafting;

public interface IDailyCraftingClient
    : IAllExpandableClient<Contract.DailyCrafting.DailyCrafting>,
        IBulkExpandableClient<Contract.DailyCrafting.DailyCrafting, string>,
        IPaginatedClient<Contract.DailyCrafting.DailyCrafting>;
