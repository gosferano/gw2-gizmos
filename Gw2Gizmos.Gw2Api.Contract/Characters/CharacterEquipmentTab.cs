namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterEquipmentTab
{
    public int Tab { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public CharacterEquipmentItem[] Equipment { get; set; } = Array.Empty<CharacterEquipmentItem>();
    public CharacterEquipmentPvp EquipmentPvp { get; set; }
}
