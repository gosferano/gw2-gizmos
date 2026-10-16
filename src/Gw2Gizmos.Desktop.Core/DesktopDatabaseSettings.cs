using Gw2Gizmos.Data.Worker;
using Microsoft.Extensions.Configuration;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Which database the desktop and its worker share, resolved from launch configuration
/// (<c>--Database:Provider</c> / <c>--ConnectionStrings:Gw2GizmosDb</c>, or matching env vars).
/// </summary>
/// <param name="Provider">The EF provider key (default <see cref="ActiveDbProvider.DefaultProviderKey"/>).</param>
/// <param name="WorkerConnectionString">Read-write; forwarded to the worker, the database's sole writer.</param>
/// <param name="DesktopConnectionString">What the desktop itself opens — read-only for the default SQLite file.</param>
public sealed record DesktopDatabaseSettings(string Provider, string WorkerConnectionString, string DesktopConnectionString)
{
    /// <summary>
    /// Resolves the settings. SQLite needs no connection string — it defaults to <paramref name="defaultSqlitePath"/>,
    /// the per-user data file; any other provider must be given one.
    /// </summary>
    public static DesktopDatabaseSettings Resolve(IConfiguration configuration, string defaultSqlitePath)
    {
        string? configuredProvider = configuration["Database:Provider"];
        string provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? ActiveDbProvider.DefaultProviderKey
            : configuredProvider;

        bool isDefaultSqlite =
            string.Equals(provider, ActiveDbProvider.DefaultProviderKey, StringComparison.OrdinalIgnoreCase);

        string? configuredConnectionString = configuration.GetConnectionString("Gw2GizmosDb");

        // The worker is the sole writer, so it gets a read-write connection string.
        string workerConnectionString = !string.IsNullOrEmpty(configuredConnectionString)
            ? configuredConnectionString
            : isDefaultSqlite
                ? $"Data Source={defaultSqlitePath}"
                : throw new InvalidOperationException(
                    $"Database:Provider='{provider}' requires a connection string (ConnectionStrings:Gw2GizmosDb=...).");

        // The desktop never writes the database — it opens it read-only. For the default per-user SQLite file
        // we append Mode=ReadOnly (we construct that string ourselves, so no flag collision); a custom or
        // server connection string is used as supplied (read-only is enforced by its own credentials).
        string desktopConnectionString = isDefaultSqlite && string.IsNullOrEmpty(configuredConnectionString)
            ? $"{workerConnectionString};Mode=ReadOnly"
            : workerConnectionString;

        return new DesktopDatabaseSettings(provider, workerConnectionString, desktopConnectionString);
    }
}
