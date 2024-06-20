namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public class CharactersQuestsClient : BaseBlobClient<int[]>, ICharactersQuestsClient
{
    private readonly string _characterId;

    internal CharactersQuestsClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/quests";
}
