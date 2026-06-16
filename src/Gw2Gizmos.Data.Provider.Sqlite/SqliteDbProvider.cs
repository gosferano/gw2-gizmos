using Gw2Gizmos.Data.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>SQLite implementation of <see cref="IGw2GizmosDbProvider"/>.</summary>
public sealed class SqliteDbProvider : IGw2GizmosDbProvider
{
    public string Key => "sqlite";

    public ISqlDialect Dialect { get; } = new SqliteSqlDialect();

    public void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(
            connectionString,
            // Migrations live in this assembly, not the core project that defines the DbContext.
            sqlite => sqlite.MigrationsAssembly(typeof(SqliteDbProvider).Assembly.GetName().Name)
        );

        // Store DateTimeOffset as UTC ticks so SQLite can order/compare/aggregate dates server-side. Confined to
        // this provider via the model customizer — the core context and other projects stay provider-agnostic.
        options.ReplaceService<IModelCustomizer, SqliteModelCustomizer>();
    }

    public void EnsureDatabase(Gw2GizmosDbContext context)
    {
        RebuildIfMigrationsDiverged(context);
        context.Database.Migrate();
        // WAL lets the desktop UI and the background worker share the file (concurrent readers + a writer).
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }

    /// <summary>
    /// Self-heal for a migrations reset: if the database has migrations applied that this build no longer ships
    /// (its history diverged — e.g. an InitialCreate was regenerated), its schema can't be brought forward and
    /// <see cref="RelationalDatabaseFacadeExtensions.Migrate"/> would fail trying to recreate existing tables.
    /// Rename the old database aside (to <c>*.bak</c>) so the user can still recover it, then let the caller
    /// re-create a fresh one from the current migrations (the worker re-syncs the data). Deliberately narrow — it
    /// only fires on genuine divergence, never on a transient/locked-file error, so it can't disturb a healthy
    /// database.
    /// </summary>
    private static void RebuildIfMigrationsDiverged(Gw2GizmosDbContext context)
    {
        IEnumerable<string> applied;
        try
        {
            applied = context.Database.GetAppliedMigrations();
        }
        catch (Exception)
        {
            // No reachable migration history yet (fresh database) — nothing to reconcile.
            return;
        }

        var known = context.Database.GetMigrations().ToHashSet();
        if (!applied.Any(id => !known.Contains(id)))
        {
            return;
        }

        string? dbPath = context.Database.GetDbConnection().DataSource;
        // Release pooled file handles so the database files can be moved.
        context.Database.GetDbConnection().Close();
        SqliteConnection.ClearAllPools();

        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
        {
            // Can't locate the file (e.g. an in-memory database) — fall back to a plain drop.
            context.Database.EnsureDeleted();
            return;
        }

        try
        {
            // Move the database and its WAL/SHM sidecars together, keeping the trio's names paired so the backup
            // stays openable. The timestamp keeps each rebuild's backup distinct (e.g. gw2gizmos.sqlite.20260616-152230.bak).
            string backupBase = $"{dbPath}.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            foreach (string suffix in new[] { "", "-wal", "-shm" })
            {
                string source = dbPath + suffix;
                if (File.Exists(source))
                {
                    File.Move(source, backupBase + suffix, overwrite: true);
                }
            }
        }
        catch (Exception)
        {
            // Couldn't rename (e.g. still locked) — drop instead so the worker isn't left stuck.
            context.Database.EnsureDeleted();
        }
    }
}
