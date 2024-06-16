namespace Gw2Gizmos.Gw2Api.Client.Clients.DailyCrafting;

public class DailyCraftingClient : BaseBulkAllClient<Contract.DailyCrafting.DailyCrafting, string>, IDailyCraftingClient
{
    internal DailyCraftingClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/dailycrafting";
}
