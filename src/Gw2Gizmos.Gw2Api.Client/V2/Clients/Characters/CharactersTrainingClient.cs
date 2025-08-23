using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public class CharactersTrainingClient : BaseBlobClient<CharacterTraining>, ICharactersTrainingClient
{
    private readonly string _characterId;

    internal CharactersTrainingClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/training";
}
