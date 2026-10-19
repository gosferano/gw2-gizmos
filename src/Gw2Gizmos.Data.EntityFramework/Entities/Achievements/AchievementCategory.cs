using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Achievements;

/// <summary>
/// An achievement category from <c>/v2/achievements/categories</c> (master data, refreshed daily), e.g. "Slayer"
/// within General, with the achievements it lists.
/// </summary>
[Table("AchievementCategories")]
public class AchievementCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(512)]
    public string Description { get; set; } = "";

    /// <summary>The category's position within its group in the game's panel.</summary>
    public int Order { get; set; }

    [MaxLength(256)]
    public string Icon { get; set; } = "";

    /// <summary>The category's achievements, in the API's order (stored as a JSON array).</summary>
    public int[] AchievementIds { get; set; } = [];
}
