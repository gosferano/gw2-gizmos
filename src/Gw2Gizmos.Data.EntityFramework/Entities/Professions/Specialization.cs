using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Professions;

/// <summary>
/// A specialization from <c>/v2/specializations</c> (master data, refreshed daily). An elite specialization (e.g.
/// Firebrand) also carries the profession icon shown for characters running it.
/// </summary>
[Table("Specializations")]
public class Specialization
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = "";

    /// <summary>The profession id it belongs to, e.g. "Guardian".</summary>
    [MaxLength(32)]
    public string Profession { get; set; } = "";

    public bool Elite { get; set; }

    [MaxLength(256)]
    public string Icon { get; set; } = "";

    /// <summary>The elite specialization's profession icon; null for core specializations.</summary>
    [MaxLength(256)]
    public string? ProfessionIcon { get; set; }

    [MaxLength(256)]
    public string? ProfessionIconBig { get; set; }
}
