namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Price alerts persist across restarts and can be edited and removed.</summary>
public sealed class PriceAlertStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    public PriceAlertStoreTests()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void Alerts_persist_and_can_be_edited_and_removed()
    {
        var paths = new AppPaths(_dataDirectory);
        var store = new PriceAlertStore(paths);
        var coin = new PriceAlert { ItemId = 19976, ItemName = "Mystic Coin", Side = PriceAlertSide.Sell, Direction = PriceAlertDirection.Below, PriceCopper = 10_000 };
        var ecto = new PriceAlert { ItemId = 19721, ItemName = "Glob of Ectoplasm", Side = PriceAlertSide.Buy, Direction = PriceAlertDirection.Above, PriceCopper = 2_500, Repeat = true };
        store.Save(coin);
        store.Save(ecto);
        store.Save(coin with { PriceCopper = 9_000 }); // an edit, not a third alert

        var reloaded = new PriceAlertStore(paths);
        Assert.Equal(new[] { coin with { PriceCopper = 9_000 }, ecto }, reloaded.All);
        Assert.Contains("\"Sell\"", File.ReadAllText(paths.File("price-alerts.json")));

        reloaded.Remove(coin.Id);
        Assert.Equal(new[] { ecto }, new PriceAlertStore(paths).All);
    }

    [Fact]
    public void Update_ignores_alerts_removed_in_the_meantime()
    {
        var store = new PriceAlertStore(new AppPaths(_dataDirectory));
        var alert = new PriceAlert { ItemId = 1, PriceCopper = 100 };
        store.Save(alert);
        store.Remove(alert.Id);

        store.Update(new[] { alert with { LastPrice = 90 } });

        Assert.Empty(store.All);
    }
}
