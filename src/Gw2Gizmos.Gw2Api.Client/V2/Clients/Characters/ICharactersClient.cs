using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public interface ICharactersClient
    : IAllExpandableClient<Character>,
        IBulkExpandableClient<Character, string>,
        IPaginatedClient<Character>
{
    ICharactersIdClient this[string characterId] { get; }
}
