using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// The account value recorder: values holdings by container at instant-sell prices plus open orders, keeps one
/// point per hour, and backfills one point per day from the holdings logs the first time.
/// </summary>
public sealed class AccountValueUpdaterTests : IDisposable
{
    private const string AccountId = "acc";

    private readonly WorkerDbFixture _fixture = new();
    private readonly MutableTimeProvider _time = new();

    public void Dispose() => _fixture.Dispose();

    private async Task RecordAsync()
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        await new AccountValueUpdater(db, _time, NullLogger<AccountValueUpdater>.Instance).RecordAsync(CancellationToken.None);
    }

    private void Seed(Action<Gw2GizmosDbContext> seed)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        seed(db);
        db.SaveChanges();
    }

    private void SeedAccount() =>
        Seed(db => db.Accounts.Add(new Account { AccountId = AccountId, Name = "Tester.1234", LastSyncedUtc = _time.Now }));

    private static AccountItemObservation Held(string container, int itemId, int count, DateTimeOffset at) =>
        new() { AccountId = AccountId, Container = container, ItemId = itemId, Count = count, ObservedAtUtc = at };

    private static AccountWalletObservation Coin(long copper, DateTimeOffset at) =>
        new() { AccountId = AccountId, CurrencyId = AccountValuation.CoinCurrencyId, Value = copper, ObservedAtUtc = at };

    private static Item Item(int id, int vendorValue) =>
        new() { Id = id, ChatLink = "", Name = $"Item {id}", Type = "Trophy", Rarity = "Basic", VendorValue = vendorValue };

    private static PriceSnapshot Price(int itemId, int buy, DateTimeOffset at) =>
        new() { ItemId = itemId, TimestampUtc = at, Buy = buy, Sell = buy + 1 };

    private List<AccountValueSnapshot> Snapshots() =>
        _fixture.NewContext().AccountValueSnapshots.AsNoTracking().OrderBy(s => s.TakenAtUtc).ToList();

    [Fact]
    public async Task Records_the_value_by_where_it_is_held()
    {
        DateTimeOffset at = _time.Now.AddMinutes(-1);
        SeedAccount();
        Seed(db =>
        {
            db.Items.Add(Item(1, vendorValue: 10)); // priced on the TP: 100 buy → 85 after the fee
            db.Items.Add(Item(2, vendorValue: 7)); // unpriced → vendor value
            db.PriceSnapshots.Add(Price(1, buy: 100, at));
            db.AccountWalletObservations.Add(Coin(5_000, at));
            db.AccountItemObservations.AddRange(
                Held(AccountContainer.MaterialStorage, 1, 10, at),
                Held(AccountContainer.Bank, 1, 1, at),
                Held(AccountContainer.Bank, 2, 3, at),
                Held(AccountContainer.SharedInventory, 2, 1, at),
                Held(AccountContainer.CharacterInventory, 1, 2, at));
            db.TradingPostOrders.AddRange(
                new TradingPostOrder { Id = 1, AccountId = AccountId, Side = TradingPostSide.Buy, ItemId = 1, Price = 50, Quantity = 4 },
                new TradingPostOrder { Id = 2, AccountId = AccountId, Side = TradingPostSide.Sell, ItemId = 1, Price = 100, Quantity = 2 });
        });

        await RecordAsync();

        AccountValueSnapshot live = Snapshots().Single(s => !s.IsBackfilled);
        Assert.Equal(5_000, live.CoinCopper);
        Assert.Equal(850, live.MaterialsCopper);
        Assert.Equal(85 + 21, live.BankCopper);
        Assert.Equal(7, live.SharedInventoryCopper);
        Assert.Equal(170, live.CharactersCopper);
        // 4 × 50 held in the buy order, and 2 × 100 listed paying out 90%.
        Assert.Equal(200 + 180, live.TradingPostCopper);
    }

    [Fact]
    public async Task Records_equipment_the_legendary_armory_and_unlocks_live()
    {
        DateTimeOffset at = _time.Now.AddMinutes(-1);
        SeedAccount();
        Seed(db =>
        {
            db.Items.AddRange(Item(1, vendorValue: 10), Item(2, vendorValue: 20), Item(3, vendorValue: 0));
            db.PriceSnapshots.Add(Price(3, buy: 1_000, at));
            db.Minis.Add(new Mini { Id = 5, Name = "Mini", ItemId = 3 });
            db.AccountUnlocks.Add(
                new AccountUnlock { AccountId = AccountId, Kind = AccountUnlockKind.Mini, UnlockId = 5 });
            db.AccountLegendaryArmory.Add(new AccountLegendaryItem { AccountId = AccountId, ItemId = 2, Count = 3 });
            db.CharacterEquippedItems.Add(new CharacterEquippedItem
            {
                AccountId = AccountId, CharacterName = "Alice", Index = 0, ItemId = 1, Part = EquippedPart.Item,
                Location = "Equipped",
            });
        });

        await RecordAsync();

        AccountValueSnapshot live = Snapshots().Single(s => !s.IsBackfilled);
        Assert.Equal((10, 60, 850), (live.EquipmentCopper, live.LegendaryArmoryCopper, live.UnlocksCopper));
    }

    [Fact]
    public async Task Keeps_one_point_per_hour()
    {
        SeedAccount();
        Seed(db => db.AccountWalletObservations.Add(Coin(100, _time.Now)));
        _time.Advance(TimeSpan.FromMinutes(1));

        await RecordAsync();
        Seed(db => db.AccountWalletObservations.Add(Coin(300, _time.Now)));
        _time.Advance(TimeSpan.FromSeconds(1));
        await RecordAsync(); // same hour: updates its point

        Assert.Equal(300, Snapshots().Single(s => !s.IsBackfilled).CoinCopper);

        _time.Advance(TimeSpan.FromHours(1));
        Seed(db => db.Accounts.Single().LastSyncedUtc = _time.Now);
        await RecordAsync(); // next hour: a new point

        Assert.Equal(2, Snapshots().Count(s => !s.IsBackfilled));
    }

    [Fact]
    public async Task Backfills_a_point_per_day_at_that_days_prices_the_first_time()
    {
        DateTimeOffset today = new(_time.Now.UtcDateTime.Date, TimeSpan.Zero);
        DateTimeOffset threeDaysAgo = today.AddDays(-3).AddHours(10);
        SeedAccount();
        Seed(db =>
        {
            db.Items.Add(Item(1, vendorValue: 1));
            db.PriceSnapshots.Add(Price(1, buy: 100, threeDaysAgo));
            db.PriceSnapshots.Add(Price(1, buy: 200, today.AddDays(-1).AddHours(12)));
            db.AccountItemObservations.Add(Held(AccountContainer.Bank, 1, 1, threeDaysAgo));
            db.AccountItemObservations.Add(Held(AccountContainer.Bank, 1, 2, today.AddDays(-2).AddHours(5)));
        });

        await RecordAsync();

        List<AccountValueSnapshot> backfilled = Snapshots().Where(s => s.IsBackfilled).ToList();
        // Midnights after the first observation, up to today's.
        Assert.Equal(new[] { today.AddDays(-2), today.AddDays(-1), today }, backfilled.Select(s => s.TakenAtUtc));
        Assert.Equal(new long[] { 85, 170, 340 }, backfilled.Select(s => s.BankCopper)); // 1 × 85, 2 × 85, 2 × 170
        Assert.All(backfilled, s => Assert.Equal(0, s.TradingPostCopper));

        // A later run doesn't backfill again.
        _time.Advance(TimeSpan.FromHours(2));
        Seed(db => db.Accounts.Single().LastSyncedUtc = _time.Now);
        await RecordAsync();
        Assert.Equal(3, Snapshots().Count(s => s.IsBackfilled));
    }

    [Fact]
    public async Task Skips_accounts_not_synced_lately()
    {
        SeedAccount();
        Seed(db => db.AccountWalletObservations.Add(Coin(100, _time.Now)));
        _time.Advance(TimeSpan.FromHours(2));

        await RecordAsync();

        Assert.Empty(Snapshots());
    }
}
