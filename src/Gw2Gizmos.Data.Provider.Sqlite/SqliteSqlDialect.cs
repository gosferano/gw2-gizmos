using Gw2Gizmos.Data.EntityFramework;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>SQLite time-bucket expressions (<c>strftime</c>/<c>date</c>) for the retention sweep's raw SQL.</summary>
internal sealed class SqliteSqlDialect : ISqlDialect
{
    public string HourBucket(string qualifier) => $"strftime('%Y-%m-%d %H', {qualifier}.TimestampUtc)";

    public string DayBucket(string qualifier) => $"date({qualifier}.TimestampUtc)";
}
