namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Craft from stock's filters persist, and only offered minimums are kept.</summary>
public sealed class CraftFromStockSettingsStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    public CraftFromStockSettingsStoreTests()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    [Fact]
    public void The_filters_start_at_10s_and_craftable_and_persist_across_instances()
    {
        var paths = new AppPaths(_dataDirectory);
        var store = new CraftFromStockSettingsStore(paths);
        Assert.Equal((1_000L, true), (store.Minimum.Copper, store.OnlyCraftable));

        store.Minimum = CraftFromStockViewModel.Minimums[^1];
        store.OnlyCraftable = false;

        var reloaded = new CraftFromStockSettingsStore(paths);
        Assert.Equal((10_000L, false), (reloaded.Minimum.Copper, reloaded.OnlyCraftable));
    }

    [Fact]
    public void A_minimum_that_is_not_offered_falls_back_to_10s()
    {
        var paths = new AppPaths(_dataDirectory);
        File.WriteAllText(paths.File("craft-from-stock-settings.json"), """{"MinimumCopper":7,"OnlyCraftable":false}""");

        var store = new CraftFromStockSettingsStore(paths);

        Assert.Equal((1_000L, false), (store.Minimum.Copper, store.OnlyCraftable));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);
}
