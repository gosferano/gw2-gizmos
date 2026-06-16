using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>
/// Lets <c>dotnet ef</c> build the context for SQLite migrations without a runnable host. The connection
/// string is a throwaway — only the provider + migrations assembly matter for generating migrations.
/// This project is its own startup for the tools (it carries the Design package), so run with:
/// <c>dotnet ef migrations add &lt;Name&gt; --project src/Gw2Gizmos.Data.Provider.Sqlite
/// --startup-project src/Gw2Gizmos.Data.Provider.Sqlite</c> (or just use <c>scripts/add-migration.ps1</c>).
/// </summary>
public sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<Gw2GizmosDbContext>
{
    public Gw2GizmosDbContext CreateDbContext(string[] args)
    {
        // Go through the provider's own Configure so the design-time model matches runtime — including the
        // DateTimeOffset→ticks converter, so generated migrations have the right (INTEGER) column types.
        var builder = new DbContextOptionsBuilder<Gw2GizmosDbContext>();
        new SqliteDbProvider().Configure(builder, "Data Source=designtime.sqlite");
        return new Gw2GizmosDbContext(builder.Options);
    }
}
