using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// An item a character has equipped, in any equipment tab (once, however many tabs it's in), or an upgrade or
/// infusion slotted in one, replaced with the character's bags each sync. Legendaries equipped from the legendary
/// armory are kept with their <see cref="Location"/>, as they're valued with the armory rather than here.
/// </summary>
[PrimaryKey(nameof(CharacterName), nameof(Index))]
public class CharacterEquippedItem
{
    [MaxLength(64)]
    public string CharacterName { get; set; } = "";

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary>The row's position among the character's equipment, its upgrades and infusions.</summary>
    public int Index { get; set; }

    public int ItemId { get; set; }

    /// <summary>What it is: <see cref="EquippedPart"/>.</summary>
    [MaxLength(16)]
    public string Part { get; set; } = "";

    /// <summary>
    /// Where the equipped item is, as the API says: "Equipped", "Armory", "EquippedFromLegendaryArmory", ….
    /// </summary>
    [MaxLength(32)]
    public string Location { get; set; } = "";
}

/// <summary>The parts of a <see cref="CharacterEquippedItem"/>.</summary>
public static class EquippedPart
{
    public const string Item = "Item";
    public const string Upgrade = "Upgrade";
    public const string Infusion = "Infusion";
}
