using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Provider.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// A real <see cref="Gw2GizmosDbContext"/> backed by a throwaway temp-file SQLite database, migrated with the
/// production InitialCreate migration so tests exercise the real schema and the DateTimeOffset-as-ticks converter.
/// Reusable across the worker test phases (account sync now; deleter / session tracker later) — instantiate it in
/// a <c>using</c>, or derive a class from it for an <see cref="IDisposable"/> per-test database.
/// </summary>
public class WorkerDbFixture : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<Gw2GizmosDbContext> _options;

    /// <summary>Connection string for the temp database — for tests that build their own DI container/scope factory
    /// over the same file (e.g. the session tracker, which resolves scoped contexts itself).</summary>
    public string ConnectionString => $"Data Source={_dbPath}";

    /// <summary>A long-lived context for reading assertions. Each sync should use a fresh <see cref="NewContext"/>
    /// instead, mirroring the worker's scoped-per-cycle DbContext (so stale change-tracking can't mask bugs).</summary>
    public Gw2GizmosDbContext Db { get; }

    public WorkerDbFixture()
    {
        // Unique file per fixture so parallel tests never share a database.
        _dbPath = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}.sqlite");

        var optionsBuilder = new DbContextOptionsBuilder<Gw2GizmosDbContext>();
        new SqliteDbProvider().Configure(optionsBuilder, $"Data Source={_dbPath}");
        _options = optionsBuilder.Options;

        Db = new Gw2GizmosDbContext(_options);
        Db.Database.Migrate(); // Applies InitialCreate — builds the real schema and wires the ticks converter.
    }

    /// <summary>A fresh context over the same database file — what each worker sync cycle gets in production.</summary>
    public Gw2GizmosDbContext NewContext() => new(_options);

    public void Dispose()
    {
        Db.Dispose();
        // Release pooled connections so the file handles drop and the temp files can be deleted.
        SqliteConnection.ClearAllPools();
        foreach (string suffix in new[] { "", "-wal", "-shm" })
        {
            string path = _dbPath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        GC.SuppressFinalize(this);
    }
}
