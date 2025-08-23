using Gw2Gizmos.Gw2Api.Contract.V2.Pets;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pets;

public class PetsClient : BaseBulkAllClient<Pet, int>, IPetsClient
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PetsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/pets";
}
