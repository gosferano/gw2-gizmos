using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Achievements;

/// <summary>
/// A top-level achievement group from <c>/v2/achievements/groups</c> (master data, refreshed daily), e.g. "Story
/// Journal" or "Fractals", as in the game's achievement panel.
/// </summary>
[Table("AchievementGroups")]
public class AchievementGroup
{
    /// <summary>The API's group id, a GUID.</summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(512)]
    public string Description { get; set; } = "";

    /// <summary>The group's position in the game's panel.</summary>
    public int Order { get; set; }

    /// <summary>The group's categories, in the API's order (stored as a JSON array).</summary>
    public int[] CategoryIds { get; set; } = [];
}
