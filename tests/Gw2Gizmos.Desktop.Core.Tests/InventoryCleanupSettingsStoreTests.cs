namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Inventory cleanup's storage capacity and kept items persist; only offered capacities are kept.</summary>
public sealed class InventoryCleanupSettingsStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    public InventoryCleanupSettingsStoreTests()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    [Fact]
    public void The_capacity_and_kept_items_persist_across_instances()
    {
        var paths = new AppPaths(_dataDirectory);
        var store = new InventoryCleanupSettingsStore(paths);
        Assert.Null(store.StorageCapacity);
        Assert.Empty(store.Kept);

        store.StorageCapacity = 1_500;
        store.Keep(19721, true);
        store.Keep(24, true);
        store.Keep(24, false);

        var reloaded = new InventoryCleanupSettingsStore(paths);
        Assert.Equal(1_500, reloaded.StorageCapacity);
        Assert.Equal([19721], reloaded.Kept);
    }

    [Fact]
    public void A_capacity_that_is_not_offered_falls_back_to_guessing()
    {
        var paths = new AppPaths(_dataDirectory);
        File.WriteAllText(paths.File("inventory-cleanup-settings.json"), """{"StorageCapacity":1234,"Kept":[7]}""");

        var store = new InventoryCleanupSettingsStore(paths);

        Assert.Null(store.StorageCapacity);
        Assert.Equal([7], store.Kept);
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);
}
