using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Professions;

/// <summary>A profession from <c>/v2/professions</c> (master data, refreshed daily): its name and icons.</summary>
[Table("Professions")]
public class Profession
{
    /// <summary>The API's profession id, e.g. "Guardian" — the same value characters store as their profession.</summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = "";

    [MaxLength(32)]
    public string Name { get; set; } = "";

    [MaxLength(256)]
    public string Icon { get; set; } = "";

    [MaxLength(256)]
    public string IconBig { get; set; } = "";
}
