using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Lets <c>dotnet ef</c> build the context for migrations without a runnable host (the engine is a
/// library now). The connection string is a throwaway — only the SQLite provider matters for
/// generating migrations.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Gw2GizmosDbContext>
{
    public Gw2GizmosDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<Gw2GizmosDbContext> options = new DbContextOptionsBuilder<Gw2GizmosDbContext>()
            .UseSqlite("Data Source=designtime.sqlite")
            .Options;

        return new Gw2GizmosDbContext(options);
    }
}
