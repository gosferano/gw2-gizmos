namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdHeroPointsClient : BaseBlobClient<string[]>, ICharactersIdHeroPointsClient
{
    private readonly string _characterId;

    internal CharactersIdHeroPointsClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/heropoints";
}
