namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public sealed class CharacterEquipmentPvp
{
    public int? Amulet { get; set; }
    public int? Rune { get; set; }
    public int?[] Sigils { get; set; } = Array.Empty<int?>();
}
