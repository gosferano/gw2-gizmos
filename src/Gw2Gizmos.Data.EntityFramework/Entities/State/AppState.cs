using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.State;

/// <summary>
/// Generic key-value store for small pieces of application/runtime state — e.g. the last-seen
/// baselines used to detect changes between polls. Keyed by an opaque string so each feature owns
/// its own key without needing a dedicated table.
/// </summary>
[Table("AppState")]
public class AppState
{
    [Key]
    [MaxLength(256)]
    public string Key { get; set; } = "";

    public string Value { get; set; } = "";
}
