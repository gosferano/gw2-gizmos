using System;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>The periods the trading post's figures cover, from local midnight.</summary>
public static class TradingPostPeriods
{
    /// <summary>The start of the current week (Monday), in local time.</summary>
    public static DateTimeOffset WeekStart(DateTimeOffset now)
    {
        DateTime today = now.LocalDateTime.Date;
        int sinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return new DateTimeOffset(today.AddDays(-sinceMonday));
    }

    /// <summary>The start of the current month, in local time.</summary>
    public static DateTimeOffset MonthStart(DateTimeOffset now)
    {
        DateTime today = now.LocalDateTime.Date;
        return new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
    }

    /// <summary>The start of the current year, in local time.</summary>
    public static DateTimeOffset YearStart(DateTimeOffset now) =>
        new(new DateTime(now.LocalDateTime.Year, 1, 1));
}
