using System;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// One log entry shown in the in-app viewer, from either process: its first line as <see cref="Message"/>, and any
/// further lines (e.g. an exception and its stack trace) as <see cref="Details"/>, so a multi-line entry is one row.
/// </summary>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message,
    string? Details = null
)
{
    public bool HasDetails => !string.IsNullOrEmpty(Details);

    /// <summary>How many further lines there are, e.g. "+12 lines"; empty when none.</summary>
    public string DetailsSummary =>
        HasDetails ? $"+{Details!.Count(c => c == '\n') + 1} lines" : "";

    /// <summary>The whole entry: the message, then its further lines.</summary>
    public string FullText => HasDetails ? $"{Message}\n{Details}" : Message;
}
