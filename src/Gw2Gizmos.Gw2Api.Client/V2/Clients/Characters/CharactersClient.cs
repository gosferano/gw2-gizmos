using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public class CharactersClient : BaseBulkAllClient<Character, string>, ICharactersClient
{
    public ICharactersIdClient this[string characterId] => new CharactersIdClient(HttpClient, characterId);

    internal CharactersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/characters";
}
