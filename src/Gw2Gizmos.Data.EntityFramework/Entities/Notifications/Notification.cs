using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Notifications;

/// <summary>
/// A user-facing notification, persisted so the in-app feed has history and so a notification
/// produced by one process (e.g. the ingestion worker) can be surfaced by another (Gw2Gizmos). Written
/// by every notifier producer; read by the Gw2Gizmos UI. <see cref="Source"/> identifies the producing
/// process so a consumer can tell its own notifications apart from another process's.
/// </summary>
[Table("Notifications")]
public class Notification
{
    public int Id { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    [MaxLength(64)]
    public string Source { get; set; } = "";

    [MaxLength(64)]
    public string Category { get; set; } = "";

    [MaxLength(256)]
    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public bool IsRead { get; set; }
}
