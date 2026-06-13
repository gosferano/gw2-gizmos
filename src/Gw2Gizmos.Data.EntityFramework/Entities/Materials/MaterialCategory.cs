using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Materials;

/// <summary>
/// A material-storage category from <c>/v2/materials</c> (master data, refreshed on the items cadence). Drives
/// the in-game-style grouped grid on the Account screen; the per-category item membership lives in
/// <see cref="MaterialCategoryItem"/>.
/// </summary>
[Table("MaterialCategories")]
public class MaterialCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    /// <summary>Display order of the category in material storage.</summary>
    public int Order { get; set; }
}
