namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersHeroPointsClient : BaseBlobClient<string[]>, ICharactersHeroPointsClient
{
    private readonly string _characterId;

    internal CharactersHeroPointsClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/heropoints";
}
