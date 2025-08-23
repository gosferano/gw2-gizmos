using Gw2Gizmos.Gw2Api.Contract.V2.Account;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public class CharacterInventoryBag
{
    public int Id { get; set; }
    public int Size { get; set; }
    public AccountItem?[] Inventory { get; set; } = Array.Empty<AccountItem?>();
}
