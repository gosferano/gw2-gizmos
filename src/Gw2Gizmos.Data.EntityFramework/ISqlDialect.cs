namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Provider-specific SQL fragments used by raw queries that have no LINQ equivalent. Today this is only the
/// time-bucket expressions used by the price-history retention sweep (SQLite <c>strftime</c>/<c>date</c>;
/// PostgreSQL would use <c>date_trunc</c>).
/// </summary>
public interface ISqlDialect
{
    /// <summary>SQL expression bucketing <c>{qualifier}.TimestampUtc</c> to the hour (for GROUP BY / matching).</summary>
    string HourBucket(string qualifier);

    /// <summary>SQL expression bucketing <c>{qualifier}.TimestampUtc</c> to the day.</summary>
    string DayBucket(string qualifier);
}
