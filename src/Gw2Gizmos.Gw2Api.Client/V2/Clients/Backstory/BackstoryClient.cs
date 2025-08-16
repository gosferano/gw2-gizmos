namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;

public class BackstoryClient : BaseBlobClient<string[]>, IBackstoryClient
{
    internal BackstoryClient(HttpClient httpClient)
        : base(httpClient)
    {
        Answers = new BackstoryAnswersClient(httpClient);
        Questions = new BackstoryQuestionsClient(httpClient);
    }

    protected override string UriPath => "/v2/backstory";

    public IBackstoryAnswersClient Answers { get; }
    public IBackstoryQuestionsClient Questions { get; }
}
