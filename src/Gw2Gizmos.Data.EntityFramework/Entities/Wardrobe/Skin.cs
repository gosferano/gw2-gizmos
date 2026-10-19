using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

/// <summary>
/// A wardrobe skin from <c>/v2/skins</c> (master data, refreshed daily), with its type-specific details: an armor
/// skin's slot, weight class and dye slots, a weapon skin's type and damage type, a gathering tool's type.
/// </summary>
[Table("Skins")]
public class Skin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(256)]
    public string Name { get; set; } = "";

    /// <summary>"Armor", "Weapon", "Back", "Gathering", …</summary>
    [MaxLength(32)]
    public string Type { get; set; } = "";

    /// <summary>The API's flags, e.g. "ShowInWardrobe", "NoCost" (stored as a JSON array).</summary>
    public string[] Flags { get; set; } = [];

    /// <summary>The races that can wear it; empty when any can (stored as a JSON array).</summary>
    public string[] Restrictions { get; set; } = [];

    [MaxLength(256)]
    public string? Icon { get; set; }

    [MaxLength(32)]
    public string Rarity { get; set; } = "";

    public string Description { get; set; } = "";

    /// <summary>An armor skin's slot, e.g. "Coat"; null for other types.</summary>
    [MaxLength(32)]
    public string? ArmorSlot { get; set; }

    /// <summary>An armor skin's weight class, e.g. "Heavy"; null for other types.</summary>
    [MaxLength(32)]
    public string? WeightClass { get; set; }

    /// <summary>
    /// An armor skin's dye slots as the API gives them (<c>{"default": [...], "overrides": {"HumanMale": [...]}}</c>,
    /// a slot null where it can't be dyed), stored as JSON; null for other types.
    /// </summary>
    public string? DyeSlots { get; set; }

    /// <summary>A weapon skin's weapon type, e.g. "Greatsword"; null for other types.</summary>
    [MaxLength(32)]
    public string? WeaponType { get; set; }

    /// <summary>A weapon skin's damage type, e.g. "Fire"; null for other types.</summary>
    [MaxLength(32)]
    public string? DamageType { get; set; }

    /// <summary>A gathering tool skin's tool type, e.g. "Logging"; null for other types.</summary>
    [MaxLength(32)]
    public string? GatheringType { get; set; }
}
