using Gw2Gizmos.Gw2Api.Contract.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Backstory;

public class BackstoryAnswersClient : BaseBulkAllClient<BackstoryAnswer, string>, IBackstoryAnswersClient
{
    internal BackstoryAnswersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/backstory/answers";
}
