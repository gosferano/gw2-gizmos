using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public class AccountItem
{
    public int Id { get; set; }
    public int Count { get; set; }
    public int? Charges { get; set; }
    public int? Skin { get; set; }
    public int[] Dyes { get; set; } = Array.Empty<int>();
    public int[] Upgrades { get; set; } = Array.Empty<int>();
    public int[] UpgradeSlotIndices { get; set; } = Array.Empty<int>();
    public int[] Infusions { get; set; } = Array.Empty<int>();
    public ItemBinding? Binding { get; set; }
    public string? BoundTo { get; set; }
    public CharacterEquipmentItemStats? Stats { get; set; }
}
