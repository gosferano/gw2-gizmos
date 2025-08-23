using Gw2Gizmos.Gw2Api.Contract.V2.Titles;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Titles;

public class TitlesClient : BaseBulkAllClient<Title, int>, ITitlesClient
{
    internal TitlesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/titles";
}
