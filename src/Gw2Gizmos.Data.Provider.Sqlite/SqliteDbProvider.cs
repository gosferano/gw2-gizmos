using Gw2Gizmos.Data.EntityFramework;
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
        context.Database.Migrate();
        // WAL lets the desktop UI and the background worker share the file (concurrent readers + a writer).
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}
