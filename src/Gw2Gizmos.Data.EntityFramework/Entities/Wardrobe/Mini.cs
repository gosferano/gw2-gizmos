using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

/// <summary>A miniature from <c>/v2/minis</c> (master data, refreshed daily).</summary>
[Table("Minis")]
public class Mini
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(256)]
    public string Name { get; set; } = "";

    /// <summary>How it's unlocked, when the API says.</summary>
    public string? Unlock { get; set; }

    [MaxLength(256)]
    public string Icon { get; set; } = "";

    /// <summary>Its position in the game's miniature list.</summary>
    public int Order { get; set; }

    /// <summary>The item that unlocks it.</summary>
    public int ItemId { get; set; }
}
