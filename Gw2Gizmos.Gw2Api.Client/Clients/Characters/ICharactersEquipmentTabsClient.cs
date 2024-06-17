using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersEquipmentTabsClient
    : IAllExpandableClient<CharacterEquipmentTab>,
        IBulkExpandableClient<CharacterEquipmentTab, int>,
        IPaginatedClient<CharacterEquipmentTab>
{
    ICharactersEquipmentTabsActiveClient Active { get; }
}
