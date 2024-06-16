namespace Gw2Gizmos.Gw2Api.Client.Clients.Backstory;

public interface IBackstoryClient : IBlobClient<string[]>
{
    IBackstoryAnswersClient Answers { get; }
    IBackstoryQuestionsClient Questions { get; }
}
