namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public sealed class CharacterEquipmentTab
{
    public int Tab { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public CharacterEquipmentItem[] Equipment { get; set; } = Array.Empty<CharacterEquipmentItem>();
    public CharacterEquipmentPvp EquipmentPvp { get; set; } = null!;
}
