namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdQuestsClient : BaseBlobClient<int[]>, ICharactersIdQuestsClient
{
    private readonly string _characterId;

    internal CharactersIdQuestsClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/quests";
}
