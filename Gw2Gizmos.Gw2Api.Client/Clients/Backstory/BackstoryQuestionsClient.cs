using Gw2Gizmos.Gw2Api.Contract.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Backstory;

public class BackstoryQuestionsClient : BaseBulkAllClient<BackstoryQuestion, int>, IBackstoryQuestionsClient
{
    internal BackstoryQuestionsClient(HttpClient httpClient, string idsParameterName = "ids")
        : base(httpClient, idsParameterName) { }

    protected override string UriPath => "/v2/backstory/questions";
}
