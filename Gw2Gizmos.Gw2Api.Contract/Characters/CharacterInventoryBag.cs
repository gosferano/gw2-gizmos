using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterInventoryBag
{
    public int Id { get; set; }
    public int Size { get; set; }
    public AccountItem?[] Inventory { get; set; } = Array.Empty<AccountItem?>();
}
