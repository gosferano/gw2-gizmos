using Microsoft.Extensions.Configuration;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The desktop/worker database split: the worker gets a read-write connection string, the desktop a read-only one
/// for the default SQLite file, and a non-SQLite provider must be configured explicitly.
/// </summary>
public class DesktopDatabaseSettingsTests
{
    private const string DbPath = "/data/gw2gizmos.sqlite";

    [Fact]
    public void Unconfigured_DefaultsToSqliteFile_DesktopReadOnly()
    {
        DesktopDatabaseSettings settings = DesktopDatabaseSettings.Resolve(Config(), DbPath);

        Assert.Equal("sqlite", settings.Provider);
        Assert.Equal($"Data Source={DbPath}", settings.WorkerConnectionString);
        Assert.Equal($"Data Source={DbPath};Mode=ReadOnly", settings.DesktopConnectionString);
    }

    [Fact]
    public void ConfiguredConnectionString_IsUsedAsSuppliedByBoth()
    {
        DesktopDatabaseSettings settings = DesktopDatabaseSettings.Resolve(
            Config(("ConnectionStrings:Gw2GizmosDb", "Data Source=/elsewhere.sqlite")), DbPath);

        Assert.Equal("Data Source=/elsewhere.sqlite", settings.WorkerConnectionString);
        Assert.Equal("Data Source=/elsewhere.sqlite", settings.DesktopConnectionString);
    }

    [Fact]
    public void OtherProvider_WithConnectionString_KeepsProviderKey()
    {
        DesktopDatabaseSettings settings = DesktopDatabaseSettings.Resolve(
            Config(("Database:Provider", "postgres"), ("ConnectionStrings:Gw2GizmosDb", "Host=db")), DbPath);

        Assert.Equal("postgres", settings.Provider);
        Assert.Equal("Host=db", settings.WorkerConnectionString);
        Assert.Equal("Host=db", settings.DesktopConnectionString);
    }

    [Fact]
    public void OtherProvider_WithoutConnectionString_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => DesktopDatabaseSettings.Resolve(Config(("Database:Provider", "postgres")), DbPath));
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
