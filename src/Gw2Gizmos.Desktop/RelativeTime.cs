using System;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Human "x ago" formatting for timestamps, falling back to an absolute local date once it's a week old (where
/// "37 days ago" reads worse than the date). Pair <see cref="Format"/> with <see cref="Exact"/> as a hover tooltip
/// so the precise time is always available. Computed at call time, so re-read it when a view reloads.
/// </summary>
public static class RelativeTime
{
    /// <summary>e.g. "just now", "5m ago", "3h ago", "yesterday", "2 days ago", then "12 May 2026".</summary>
    public static string Format(DateTimeOffset whenUtc)
    {
        TimeSpan age = DateTimeOffset.UtcNow - whenUtc;
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        if (age.TotalSeconds < 60)
        {
            return "just now";
        }

        if (age.TotalMinutes < 60)
        {
            return $"{(int)age.TotalMinutes}m ago";
        }

        if (age.TotalHours < 24)
        {
            return $"{(int)age.TotalHours}h ago";
        }

        if (age.TotalDays < 7)
        {
            int days = (int)age.TotalDays;
            return days == 1 ? "yesterday" : $"{days} days ago";
        }

        return whenUtc.LocalDateTime.ToString("d MMM yyyy");
    }

    /// <summary>The exact local timestamp, for a tooltip beside the relative text.</summary>
    public static string Exact(DateTimeOffset whenUtc) => whenUtc.LocalDateTime.ToString("f");
}
