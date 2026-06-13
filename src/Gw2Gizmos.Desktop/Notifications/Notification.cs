using System;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// A user-facing notification shown in the in-app feed and persisted (newest-first) to the desktop's
/// notification history file by <see cref="FileNotifier"/>. A plain model — notifications are now a
/// desktop-only concept and are no longer stored in the worker-owned database.
/// </summary>
public sealed class Notification
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string Source { get; set; } = "";

    public string Category { get; set; } = "";

    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public bool IsRead { get; set; }
}
