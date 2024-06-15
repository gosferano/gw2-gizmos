namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterEquipmentItem
{
    public int Id { get; set; }
    public CharacterEquipmentItemSlot Slot { get; set; }
    public int[] Infusions { get; set; } = Array.Empty<int>();
    public int[] Upgrades { get; set; } = Array.Empty<int>();
    public int Skin { get; set; }
    public ItemStats? Stats { get; set; }
    public ItemBinding Binding { get; set; }
    public CharacterEquipmentLocation Location { get; set; }
    public int[] Tabs { get; set; } = Array.Empty<int>();
    public int? Charges { get; set; }
    public string? BoundTo { get; set; }
    public int?[] Dyes { get; set; } = Array.Empty<int?>();
}
