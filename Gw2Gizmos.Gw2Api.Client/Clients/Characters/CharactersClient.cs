using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersClient : BaseBulkAllClient<Character, string>, ICharactersClient
{
    public ICharactersIdClient this[string characterName] => new CharactersIdClient(ApiClient, characterName);

    internal CharactersClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/characters";
}
