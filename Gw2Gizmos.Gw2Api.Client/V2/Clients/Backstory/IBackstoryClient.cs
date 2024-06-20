namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;

public interface IBackstoryClient : IBlobClient<string[]>
{
    IBackstoryAnswersClient Answers { get; }
    IBackstoryQuestionsClient Questions { get; }
}
