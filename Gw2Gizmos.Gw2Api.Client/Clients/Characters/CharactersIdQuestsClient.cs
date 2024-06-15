namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdQuestsClient : BaseBlobClient<int[]>, ICharactersIdQuestsClient
{
    private readonly string _characterId;

    internal CharactersIdQuestsClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/quests";
}
