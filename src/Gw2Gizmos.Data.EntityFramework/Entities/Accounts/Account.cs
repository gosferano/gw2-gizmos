using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// A GW2 account whose authenticated data has been synced. Keyed by the stable account GUID from
/// <c>/v2/account</c> so several accounts can coexist; upserted on each sync. The desktop shows the
/// most-recently-synced account (the current API key's).
/// </summary>
[Table("Accounts")]
public class Account
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    [MaxLength(64)]
    public string Name { get; set; } = "";

    public int World { get; set; }

    public DateTimeOffset LastSyncedUtc { get; set; }
}
