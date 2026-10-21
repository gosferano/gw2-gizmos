using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Provider.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The desktop's readers wait for the worker to migrate the database: the schema reads as current only once every
/// migration is applied, and readers in a scope without the check (tests) are let through.
/// </summary>
public sealed class DatabaseSchemaTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-schema-{Guid.NewGuid():N}");

    public DatabaseSchemaTests()
    {
        Directory.CreateDirectory(_directory);
    }

    private ServiceProvider Services(string fileName, bool withSchemaCheck = true)
    {
        var services = new ServiceCollection();
        services.AddDbContext<Gw2GizmosDbContext>(o =>
            new SqliteDbProvider().Configure(o, $"Data Source={Path.Combine(_directory, fileName)}"));
        if (withSchemaCheck)
        {
            services.AddSingleton<DatabaseSchema>();
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public void A_database_without_its_migrations_isnt_current_and_a_migrated_one_is()
    {
        using ServiceProvider provider = Services("db.sqlite");
        var schema = provider.GetRequiredService<DatabaseSchema>();

        Assert.False(schema.IsCurrent); // the file doesn't exist yet: the worker hasn't created it

        using (IServiceScope scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>().Database.Migrate();
        }

        // Rechecked at most once a second until current.
        Thread.Sleep(TimeSpan.FromSeconds(1.1));
        Assert.True(schema.IsCurrent);
        using IServiceScope reader = provider.CreateScope();
        Assert.True(DatabaseSchema.Allows(reader));
    }

    [Fact]
    public void A_reader_without_the_check_is_let_through()
    {
        using ServiceProvider provider = Services("other.sqlite", withSchemaCheck: false);
        using IServiceScope scope = provider.CreateScope();

        Assert.True(DatabaseSchema.Allows(scope));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
