using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The Events screen's settings persist, including the selected festival, and older files still load.</summary>
public sealed class ReminderSettingsStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    public ReminderSettingsStoreTests()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    [Fact]
    public void SelectedFestival_PersistsAcrossInstances()
    {
        var paths = new AppPaths(_dataDirectory);
        var store = new ReminderSettingsStore(paths) { SelectedFestival = Festival.DragonBash, LeadTimeMinutes = 10 };

        var reloaded = new ReminderSettingsStore(paths);

        Assert.Equal(Festival.DragonBash, reloaded.SelectedFestival);
        Assert.Equal(10, reloaded.LeadTimeMinutes);
        Assert.Contains("\"DragonBash\"", File.ReadAllText(paths.File("reminder-settings.json")));

        store.SelectedFestival = null;
        Assert.Null(new ReminderSettingsStore(paths).SelectedFestival);
    }

    [Fact]
    public void A_FileFromBeforeFestivals_StillLoads()
    {
        var paths = new AppPaths(_dataDirectory);
        File.WriteAllText(paths.File("reminder-settings.json"), "{\"LeadTimeMinutes\":15}");

        var store = new ReminderSettingsStore(paths);

        Assert.Equal(15, store.LeadTimeMinutes);
        Assert.Null(store.SelectedFestival);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
