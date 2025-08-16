using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public interface ICharactersEquipmentTabsClient
    : IAllExpandableClient<CharacterEquipmentTab>,
        IBulkExpandableClient<CharacterEquipmentTab, int>,
        IPaginatedClient<CharacterEquipmentTab>
{
    ICharactersEquipmentTabsActiveClient Active { get; }
}
