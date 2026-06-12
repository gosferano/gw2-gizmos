using Gw2Gizmos.Gw2Api.Contract.V2.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;

public sealed class BackstoryAnswersClient : BaseBulkAllClient<BackstoryAnswer, string>, IBackstoryAnswersClient
{
    internal BackstoryAnswersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/backstory/answers";
}
