using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersSabClient : BaseBlobClient<CharacterSab>, ICharactersSabClient
{
    private readonly string _characterId;

    internal CharactersSabClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/sab";
}
