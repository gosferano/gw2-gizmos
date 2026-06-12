using Gw2Gizmos.Gw2Api.Contract.V2.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;

public sealed class BackstoryQuestionsClient : BaseBulkAllClient<BackstoryQuestion, int>, IBackstoryQuestionsClient
{
    internal BackstoryQuestionsClient(HttpClient httpClient, string idsParameterName = "ids")
        : base(httpClient, idsParameterName) { }

    protected override string UriPath => "/v2/backstory/questions";
}
