namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public sealed class CharactersHeroPointsClient : BaseBlobClient<string[]>, ICharactersHeroPointsClient
{
    private readonly string _characterId;

    internal CharactersHeroPointsClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/heropoints";
}
