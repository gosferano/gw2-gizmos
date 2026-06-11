using System;

namespace Gw2Gizmos.Desktop;

/// <summary>One log line shown in the in-app viewer, from either process.</summary>
public sealed record LogEntry(DateTimeOffset Timestamp, string Level, string Source, string Message);
