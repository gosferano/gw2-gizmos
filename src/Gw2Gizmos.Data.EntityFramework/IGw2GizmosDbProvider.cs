using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Abstracts the concrete database engine (SQLite today; e.g. PostgreSQL later) so the core data layer
/// carries no provider dependency. Each provider lives in its own <c>Gw2Gizmos.Data.Provider.*</c> project,
/// owns its migrations assembly, and is registered at the composition root before the data services.
/// </summary>
public interface IGw2GizmosDbProvider
{
    /// <summary>
    /// Stable identifier used to select this provider at launch via the <c>Database:Provider</c> setting
    /// (e.g. <c>"sqlite"</c>, <c>"postgres"</c>). Case-insensitive.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Configures the context options for this provider — the <c>UseXxx(connectionString)</c> call plus its
    /// migrations assembly (migrations are provider-specific and live alongside the provider).
    /// </summary>
    void Configure(DbContextOptionsBuilder options, string connectionString);

    /// <summary>
    /// Brings the database up to the current schema (applies migrations) and performs any provider-specific
    /// setup (for SQLite, switching to WAL journaling). Called once after the host is built.
    /// </summary>
    void EnsureDatabase(Gw2GizmosDbContext context);

    /// <summary>Provider-specific SQL fragments for the few raw queries that can't be expressed in LINQ.</summary>
    ISqlDialect Dialect { get; }
}
