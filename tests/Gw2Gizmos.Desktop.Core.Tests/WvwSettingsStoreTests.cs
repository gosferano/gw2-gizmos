namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The WvW page's activity window persists, and only offered spans are kept.</summary>
public sealed class WvwSettingsStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    public WvwSettingsStoreTests()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    [Fact]
    public void The_activity_window_starts_at_ten_minutes_and_persists_across_instances()
    {
        var paths = new AppPaths(_dataDirectory);
        var store = new WvwSettingsStore(paths);
        Assert.Equal(10, store.ActivityWindowMinutes);

        store.ActivityWindowMinutes = 30;

        Assert.Equal(30, new WvwSettingsStore(paths).ActivityWindowMinutes);
    }

    [Fact]
    public void A_span_that_is_not_offered_falls_back_to_ten_minutes()
    {
        var paths = new AppPaths(_dataDirectory);
        File.WriteAllText(paths.File("wvw-settings.json"), """{"ActivityWindowMinutes":7}""");

        var store = new WvwSettingsStore(paths);
        Assert.Equal(10, store.ActivityWindowMinutes);

        store.ActivityWindowMinutes = 42;
        Assert.Equal(10, store.ActivityWindowMinutes);
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);
}
