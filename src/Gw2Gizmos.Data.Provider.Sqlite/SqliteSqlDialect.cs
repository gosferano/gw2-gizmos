using Gw2Gizmos.Data.EntityFramework;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>
/// SQLite time-bucket expressions for the retention sweep's raw SQL. <c>TimestampUtc</c> is stored as UTC ticks
/// (an integer, via the model's value converter), so bucketing is integer-division of the ticks — NOT
/// <c>strftime</c>/<c>date</c>, which would misread the integer as a Julian day. Two rows share a bucket when their
/// ticks fall in the same hour/day. (Raw SQL bypasses the value converter, so the column is the raw ticks here.)
/// </summary>
internal sealed class SqliteSqlDialect : ISqlDialect
{
    private const long TicksPerHour = 36_000_000_000L;
    private const long TicksPerDay = 864_000_000_000L;

    public string HourBucket(string qualifier) => $"({qualifier}.TimestampUtc / {TicksPerHour})";

    public string DayBucket(string qualifier) => $"({qualifier}.TimestampUtc / {TicksPerDay})";
}
