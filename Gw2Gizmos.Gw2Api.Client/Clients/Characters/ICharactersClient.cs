using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersClient
    : IAllExpandableClient<Character>,
        IBulkExpandableClient<Character, string>,
        IPaginatedClient<Character>
{
    ICharactersIdClient this[string characterId] { get; }
}
