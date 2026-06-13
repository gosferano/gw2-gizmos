using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>
/// Lets <c>dotnet ef</c> build the context for SQLite migrations without a runnable host. The connection
/// string is a throwaway — only the provider + migrations assembly matter for generating migrations.
/// Run with: <c>dotnet ef migrations add &lt;Name&gt; --project src/Gw2Gizmos.Data.Provider.Sqlite
/// --startup-project src/Gw2Gizmos.Data.Worker.Cli</c>.
/// </summary>
public sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<Gw2GizmosDbContext>
{
    public Gw2GizmosDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<Gw2GizmosDbContext> options = new DbContextOptionsBuilder<Gw2GizmosDbContext>()
            .UseSqlite(
                "Data Source=designtime.sqlite",
                sqlite => sqlite.MigrationsAssembly(typeof(SqliteDbProvider).Assembly.GetName().Name)
            )
            .Options;

        return new Gw2GizmosDbContext(options);
    }
}
