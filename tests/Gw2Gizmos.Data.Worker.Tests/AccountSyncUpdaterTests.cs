using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Integration tests for the worker's account-sync write path (<see cref="AccountSyncUpdater"/>): a real
/// SQLite-backed <see cref="Gw2GizmosDbContext"/> + the real <c>Gw2ApiClient</c> over a routing HTTP stub, so the
/// whole pipeline runs except the socket. Asserts the append-on-change observation logs, the replaced slot grids,
/// and that <see cref="AccountHoldingsReconstructor"/> round-trips the as-of state back out.
/// </summary>
public sealed class AccountSyncUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();
    private readonly MutableTimeProvider _time = new();

    // Read-only context for assertions; never used to write, so it always re-reads from the file.
    private Gw2GizmosDbContext Db => _fixture.Db;

    public void Dispose() => _fixture.Dispose();

    /// <summary>Builds an updater over the given context, wired to the shared stub and time provider, all features on.</summary>
    private AccountSyncUpdater NewUpdater(Gw2GizmosDbContext db) =>
        new(
            db,
            _stub.BuildClientFactory(),
            new FakeApiKeyProvider(),
            new AllEnabledFeatureGate(),
            _time,
            NullLogger<AccountSyncUpdater>.Instance
        );

    /// <summary>A small but complete account: wallet, materials, a bank item, a shared item, one character.</summary>
    private static AccountStateBuilder SmallAccount()
    {
        var state = new AccountStateBuilder();
        state.Wallet[1] = 10_000; // coins
        state.Wallet[2] = 50; // karma-ish
        state.Materials[100] = 250;
        state.Materials[101] = 5;
        state.Bank.Add(new AccountStateBuilder.ItemSlot(Id: 500, Count: 3));
        state.Bank.Add(null); // empty slot
        state.SharedInventory.Add(new AccountStateBuilder.ItemSlot(Id: 600, Count: 1));
        state.Characters["Alice Necro"] = new List<AccountStateBuilder.ItemSlot?>
        {
            new(Id: 700, Count: 4),
            null,
            new(Id: 701, Count: 2),
        };
        return state;
    }

    /// <summary>Runs one sync against a fresh context — exactly the worker's scoped-per-cycle DbContext, so each
    /// sync sees only persisted state (no stale change-tracking carried between cycles).</summary>
    private async Task Sync()
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        await NewUpdater(db).SyncAccount(CancellationToken.None);
    }

    [Fact]
    public async Task FirstSync_PersistsHoldings()
    {
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);

        await Sync();

        // Account identity row.
        Account account = await Db.Accounts.SingleAsync();
        Assert.Equal(state.Id, account.AccountId);
        Assert.Equal("Tester.1234", account.Name);
        Assert.Equal(1001, account.World);
        Assert.Equal(_time.Now, account.LastSyncedUtc);

        // Wallet: one observation per currency.
        Assert.Equal(2, await Db.AccountWalletObservations.CountAsync());
        Assert.Equal(10_000, await WalletValue(1));
        Assert.Equal(50, await WalletValue(2));

        // Item observations: 2 materials + 1 bank + 1 shared + 2 character items = 6.
        Assert.Equal(2, await ItemObsCount(AccountContainer.MaterialStorage));
        Assert.Equal(1, await ItemObsCount(AccountContainer.Bank));
        Assert.Equal(1, await ItemObsCount(AccountContainer.SharedInventory));
        Assert.Equal(2, await ItemObsCount(AccountContainer.CharacterInventory));

        // Slot grids: bank (2 slots incl. empty), shared (1 slot), character (3 slots incl. empty).
        Assert.Equal(2, await Db.AccountContainerSlots.CountAsync(s => s.Store == AccountContainer.Bank));
        Assert.Equal(1, await Db.AccountContainerSlots.CountAsync(s => s.Store == AccountContainer.SharedInventory));
        Assert.Equal(3, await Db.CharacterItemSlots.CountAsync(s => s.CharacterName == "Alice Necro"));

        // Roster.
        Character character = await Db.Characters.SingleAsync();
        Assert.Equal("Alice Necro", character.Name);
        Assert.Equal("Necromancer", character.Profession);
        Assert.Equal("Asura", character.Race);
        Assert.Equal("Female", character.Gender);
        Assert.Equal(80, character.Level);
        Assert.Equal(new[] { 19, 50, 34 }, character.Specializations); // the active build tab's
        Assert.Equal(
            new[] { ("Scribe", 500, true), ("Chef", 400, false) },
            character.Crafting.Select(c => (c.Discipline, c.Rating, c.Active)));
    }

    [Fact]
    public async Task InterruptedCharacterSync_LeavesStoredBagsAsTheyWere()
    {
        AccountStateBuilder state = SmallAccount();
        state.Characters["Bob Guard"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 800, Count: 1) };
        state.Apply(_stub);
        await Sync();

        // Next sync: both characters' bags changed, but the sync is cut off while fetching the second character —
        // after the first one's new bags were already in hand.
        state.Characters["Alice Necro"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 900, Count: 9) };
        state.Characters["Bob Guard"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 901, Count: 9) };
        state.Apply(_stub);
        using var cancellation = new CancellationTokenSource();
        _stub.OnRequest = path =>
        {
            if (path == "/v2/characters/Bob Guard")
            {
                cancellation.Cancel();
            }
        };

        try
        {
            await using Gw2GizmosDbContext db = _fixture.NewContext();
            await NewUpdater(db).SyncAccount(cancellation.Token);
        }
        catch (OperationCanceledException) { }

        // Neither character's bags were touched: no half-replaced grids.
        Assert.Equal(new int?[] { 700, null, 701 }, await SlotItems("Alice Necro"));
        Assert.Equal(new int?[] { 800 }, await SlotItems("Bob Guard"));
    }

    [Fact]
    public async Task CharacterThatFailsToLoad_KeepsItsBagsAndItsItemsAreNotLoggedAsGone()
    {
        AccountStateBuilder state = SmallAccount();
        state.Characters["Bob Guard"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 800, Count: 1) };
        state.Apply(_stub);
        await Sync();
        int observationsBefore = await ItemObsCount(AccountContainer.CharacterInventory);

        // Bob's request now fails (an unparseable response); Alice syncs as normal.
        _stub.SetJson("/v2/characters/Bob Guard", "not json");
        _time.Advance(TimeSpan.FromHours(1));
        await Sync();

        Assert.Equal(new int?[] { 800 }, await SlotItems("Bob Guard"));
        // Bob's item still counts toward the account's holdings, so no "gone" row was appended for it.
        Assert.Equal(observationsBefore, await ItemObsCount(AccountContainer.CharacterInventory));
    }

    [Fact]
    public async Task BoundarySync_RecordsHoldingsAtTheBoundary_ForJustTheGivenCharacters()
    {
        AccountStateBuilder state = SmallAccount();
        state.Characters["Bob Guard"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 800, Count: 1) };
        state.Permissions.Add("tradingpost"); // so a full sync would read the trading post
        state.Apply(_stub);
        await Sync();
        Assert.Contains(_stub.RequestedPaths, path => path.StartsWith("/v2/commerce/"));
        int bobObservations = await Db.AccountItemObservations.CountAsync(o => o.ItemId == 800);

        // Alice picks up coin and an item; the boundary was a minute before the sync runs.
        state.Wallet[1] = 12_000;
        state.Characters["Alice Necro"] = new List<AccountStateBuilder.ItemSlot?> { new(Id: 700, Count: 9) };
        state.Apply(_stub);
        DateTimeOffset boundary = _time.Now.AddMinutes(1);
        _time.Advance(TimeSpan.FromMinutes(2));
        _stub.RequestedPaths.Clear();

        await using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            await NewUpdater(db).SyncHoldingsAt(boundary, new[] { "Alice Necro" }, CancellationToken.None);
        }

        // Only Alice was fetched, and the trading post was left alone.
        Assert.Contains("/v2/characters/Alice Necro", _stub.RequestedPaths);
        Assert.DoesNotContain("/v2/characters/Bob Guard", _stub.RequestedPaths);
        Assert.DoesNotContain(_stub.RequestedPaths, path => path.StartsWith("/v2/commerce/"));

        // The changes are stamped at the boundary, not when the sync ran.
        AccountWalletObservation coin = await Db.AccountWalletObservations.OrderByDescending(o => o.Id).FirstAsync(o => o.CurrencyId == 1);
        Assert.Equal((12_000, boundary), (coin.Value, coin.ObservedAtUtc));
        AccountItemObservation alicesItem = await Db.AccountItemObservations.OrderByDescending(o => o.Id).FirstAsync(o => o.ItemId == 700);
        Assert.Equal((9, boundary), (alicesItem.Count, alicesItem.ObservedAtUtc));

        // Bob's bags weren't touched and still count: no "gone" row for his item.
        Assert.Equal(new int?[] { 800 }, await SlotItems("Bob Guard"));
        Assert.Equal(bobObservations, await Db.AccountItemObservations.CountAsync(o => o.ItemId == 800));
        // The account's synced time is when the sync actually ran.
        Assert.Equal(_time.Now, (await Db.Accounts.SingleAsync()).LastSyncedUtc);
    }

    private Task<List<int?>> SlotItems(string characterName) =>
        Db.CharacterItemSlots.Where(s => s.CharacterName == characterName)
            .OrderBy(s => s.SlotIndex)
            .Select(s => s.ItemId)
            .ToListAsync();

    [Fact]
    public async Task SyncTwiceUnchanged_AppendsNoNewObservations()
    {
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);

        await Sync();
        int walletAfterFirst = await Db.AccountWalletObservations.CountAsync();
        int itemsAfterFirst = await Db.AccountItemObservations.CountAsync();

        _time.Advance(TimeSpan.FromHours(1));
        await Sync(); // identical data

        // Append-on-change: nothing changed, so no new observation rows.
        Assert.Equal(walletAfterFirst, await Db.AccountWalletObservations.CountAsync());
        Assert.Equal(itemsAfterFirst, await Db.AccountItemObservations.CountAsync());
    }

    [Fact]
    public async Task ItemCountChange_AppendsExactlyOneObservation()
    {
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);
        await Sync();
        int materialsBefore = await ItemObsCount(AccountContainer.MaterialStorage);

        _time.Advance(TimeSpan.FromHours(1));
        state.Materials[100] = 999; // changed; 101 unchanged
        state.Apply(_stub);
        await Sync();

        // Exactly one new material-storage row (for item 100), with the new count; item 101 got no new row.
        Assert.Equal(materialsBefore + 1, await ItemObsCount(AccountContainer.MaterialStorage));
        Assert.Equal(999, await LatestItemCount(AccountContainer.MaterialStorage, 100));
        Assert.Equal(1, await Db.AccountItemObservations.CountAsync(
            o => o.Container == AccountContainer.MaterialStorage && o.ItemId == 101));
    }

    [Fact]
    public async Task ItemRemoval_AppendsZeroObservation()
    {
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);
        await Sync();

        _time.Advance(TimeSpan.FromHours(1));
        state.Materials.Remove(101); // item 101 gone from material storage
        state.Apply(_stub);
        await Sync();

        // A new zero-count observation marks the removal.
        Assert.Equal(0, await LatestItemCount(AccountContainer.MaterialStorage, 101));
        Assert.Equal(2, await Db.AccountItemObservations.CountAsync(
            o => o.Container == AccountContainer.MaterialStorage && o.ItemId == 101));
    }

    [Fact]
    public async Task SlotGrid_IsReplacedNotAppended()
    {
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);
        await Sync();

        _time.Advance(TimeSpan.FromHours(1));
        // New bank layout: a single different item in one slot.
        state.Bank.Clear();
        state.Bank.Add(new AccountStateBuilder.ItemSlot(Id: 555, Count: 9));
        state.Apply(_stub);
        await Sync();

        List<AccountContainerSlot> bankSlots = await Db.AccountContainerSlots
            .Where(s => s.Store == AccountContainer.Bank)
            .ToListAsync();

        // Only the new layout remains — not duplicated/appended.
        AccountContainerSlot slot = Assert.Single(bankSlots);
        Assert.Equal(0, slot.SlotIndex);
        Assert.Equal(555, slot.ItemId);
        Assert.Equal(9, slot.Count);
    }

    [Fact]
    public async Task Slots_KeepWhatTheirCopyIsBoundTo()
    {
        AccountStateBuilder state = SmallAccount();
        state.Bank.Clear();
        state.Bank.Add(new AccountStateBuilder.ItemSlot(Id: 555, Count: 1, Binding: "Account"));
        state.Bank.Add(new AccountStateBuilder.ItemSlot(Id: 556, Count: 1));
        state.Characters["Alice Necro"] = [new(Id: 900, Count: 1, Binding: "Character")];
        state.Apply(_stub);

        await Sync();

        Assert.Equal(["Account", null], await Db.AccountContainerSlots
            .Where(s => s.Store == AccountContainer.Bank).OrderBy(s => s.SlotIndex).Select(s => s.Binding)
            .ToListAsync());
        Assert.Equal("Character", (await Db.CharacterItemSlots.SingleAsync(s => s.ItemId == 900)).Binding);
    }

    [Fact]
    public async Task RoundTrip_ReconstructorReturnsAsOfState()
    {
        // T0: the initial holdings.
        DateTimeOffset t0 = _time.Now;
        AccountStateBuilder state = SmallAccount();
        state.Apply(_stub);
        await Sync();

        Dictionary<int, int> itemsAtT0 = AccountHoldingsReconstructor.ItemTotalsAsOf(Db, state.Id, t0);
        // Per-item totals summed across all containers: 100,101 (materials), 500 (bank), 600 (shared), 700,701 (char).
        Assert.Equal(250, itemsAtT0[100]);
        Assert.Equal(5, itemsAtT0[101]);
        Assert.Equal(3, itemsAtT0[500]);
        Assert.Equal(1, itemsAtT0[600]);
        Assert.Equal(4, itemsAtT0[700]);
        Assert.Equal(2, itemsAtT0[701]);

        Assert.Equal(10_000, AccountHoldingsReconstructor.WalletValueAsOf(Db, state.Id, 1, t0));
        Dictionary<int, long> walletAtT0 = AccountHoldingsReconstructor.WalletTotalsAsOf(Db, state.Id, t0);
        Assert.Equal(10_000, walletAtT0[1]);
        Assert.Equal(50, walletAtT0[2]);

        // T1: change a material count and a wallet balance.
        _time.Advance(TimeSpan.FromDays(1));
        DateTimeOffset t1 = _time.Now;
        state.Materials[100] = 1_000;
        state.Wallet[1] = 25_000;
        state.Apply(_stub);
        await Sync();

        // As-of T0 still returns the original state (event sourcing).
        Dictionary<int, int> stillT0 = AccountHoldingsReconstructor.ItemTotalsAsOf(Db, state.Id, t0);
        Assert.Equal(250, stillT0[100]);
        Assert.Equal(10_000, AccountHoldingsReconstructor.WalletValueAsOf(Db, state.Id, 1, t0));

        // As-of T1 returns the new state.
        Dictionary<int, int> atT1 = AccountHoldingsReconstructor.ItemTotalsAsOf(Db, state.Id, t1);
        Assert.Equal(1_000, atT1[100]);
        Assert.Equal(25_000, AccountHoldingsReconstructor.WalletValueAsOf(Db, state.Id, 1, t1));
    }

    // --- query helpers (latest observation per key) ---

    private Task<int> ItemObsCount(string container) =>
        Db.AccountItemObservations.CountAsync(o => o.Container == container);

    private async Task<long> WalletValue(int currencyId) =>
        await Db.AccountWalletObservations
            .Where(o => o.CurrencyId == currencyId)
            .OrderByDescending(o => o.Id)
            .Select(o => o.Value)
            .FirstAsync();

    private async Task<int> LatestItemCount(string container, int itemId) =>
        await Db.AccountItemObservations
            .Where(o => o.Container == container && o.ItemId == itemId)
            .OrderByDescending(o => o.Id)
            .Select(o => o.Count)
            .FirstAsync();

    // --- trading post ---

    private static string Trade(long id, int itemId, int price, int quantity, bool completed) =>
        completed
            ? $$"""{"id":{{id}},"item_id":{{itemId}},"price":{{price}},"quantity":{{quantity}},"created":"2026-10-01T10:00:00Z","purchased":"2026-10-01T11:00:00Z"}"""
            : $$"""{"id":{{id}},"item_id":{{itemId}},"price":{{price}},"quantity":{{quantity}},"created":"2026-10-01T10:00:00Z"}""";

    private void StubTradingPost(string buys, string sells, string historyBuys, string historySells)
    {
        _stub.SetJson("/v2/commerce/transactions/current/buys", buys);
        _stub.SetJson("/v2/commerce/transactions/current/sells", sells);
        _stub.SetJson("/v2/commerce/transactions/history/buys", historyBuys);
        _stub.SetJson("/v2/commerce/transactions/history/sells", historySells);
        // Item 19721: best buy 120, best sell 150. Item 24: nobody buying, best sell 900.
        _stub.SetJson(
            "/v2/commerce/prices",
            """[{"id":19721,"whitelisted":false,"buys":{"quantity":10,"unit_price":120},"sells":{"quantity":5,"unit_price":150}},"""
                + """{"id":24,"whitelisted":false,"buys":{"quantity":0,"unit_price":0},"sells":{"quantity":3,"unit_price":900}}]""");
    }

    [Fact]
    public async Task TradingPost_StoresOpenOrdersWithCompetingPrices_AndCompletedTrades()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("tradingpost");
        state.Apply(_stub);
        StubTradingPost(
            buys: $"[{Trade(1, 19721, 110, 50, completed: false)}]",
            sells: $"[{Trade(2, 24, 1000, 2, completed: false)}]",
            historyBuys: $"[{Trade(10, 19721, 100, 25, completed: true)}]",
            historySells: $"[{Trade(11, 24, 950, 1, completed: true)}]");

        await Sync();

        TradingPostOrder buy = await Db.TradingPostOrders.SingleAsync(o => o.Id == 1);
        Assert.Equal((TradingPostSide.Buy, 110, 50, 120, 150), (buy.Side, buy.Price, buy.Quantity, buy.BestBuyPrice, buy.BestSellPrice));
        TradingPostOrder sell = await Db.TradingPostOrders.SingleAsync(o => o.Id == 2);
        Assert.Equal((TradingPostSide.Sell, 1000, (int?)null, 900), (sell.Side, sell.Price, sell.BestBuyPrice, sell.BestSellPrice));

        List<TradingPostTransaction> trades = await Db.TradingPostTransactions.OrderBy(t => t.Id).ToListAsync();
        Assert.Equal([(10L, TradingPostSide.Buy), (11L, TradingPostSide.Sell)], trades.Select(t => (t.Id, t.Side)));
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 11, 0, 0, TimeSpan.Zero), trades[0].PurchasedUtc);
        Assert.All(trades, t => Assert.Equal(state.Id, t.AccountId));
    }

    [Fact]
    public async Task TradingPost_LaterSync_ReplacesOrders_AndAddsOnlyNewTrades()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("tradingpost");
        state.Apply(_stub);
        StubTradingPost(
            buys: $"[{Trade(1, 19721, 110, 50, completed: false)}]",
            sells: "[]",
            historyBuys: $"[{Trade(10, 19721, 100, 25, completed: true)}]",
            historySells: "[]");
        await Sync();

        // The buy order filled; a new trade appears above the known one (history is newest first).
        StubTradingPost(
            buys: "[]",
            sells: $"[{Trade(3, 24, 1000, 1, completed: false)}]",
            historyBuys: $"[{Trade(12, 19721, 110, 50, completed: true)}, {Trade(10, 19721, 100, 25, completed: true)}]",
            historySells: "[]");
        await Sync();

        Assert.Equal([3L], await Db.TradingPostOrders.Select(o => o.Id).ToListAsync());
        Assert.Equal([10L, 12L], await Db.TradingPostTransactions.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync());
    }

    [Fact]
    public async Task TradingPost_WithoutThePermission_IsSkipped()
    {
        AccountStateBuilder state = SmallAccount(); // no "tradingpost" permission
        state.Apply(_stub);
        StubTradingPost(
            buys: $"[{Trade(1, 19721, 110, 50, completed: false)}]",
            sells: "[]",
            historyBuys: $"[{Trade(10, 19721, 100, 25, completed: true)}]",
            historySells: "[]");

        await Sync();

        Assert.Equal(0, await Db.TradingPostOrders.CountAsync());
        Assert.Equal(0, await Db.TradingPostTransactions.CountAsync());
    }

    [Fact]
    public async Task Achievements_StoresProgress_AndALaterSyncReplacesIt()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        _stub.SetJson(
            "/v2/account/achievements",
            """
            [{"id": 1, "current": 120, "max": 1000, "done": false},
             {"id": 2258, "bits": [0, 4], "current": 2, "max": 7, "done": false, "unlocked": true}]
            """
        );
        await Sync();

        AccountAchievement collection = await Db.AccountAchievements.SingleAsync(a => a.AchievementId == 2258);
        Assert.Equal((state.Id, 2, 7, false, true), (collection.AccountId, collection.Current, collection.Max,
            collection.Done, collection.Unlocked));
        Assert.Equal([0, 4], collection.Bits);

        _stub.SetJson("/v2/account/achievements", """[{"id": 1, "current": 1000, "max": 1000, "done": true}]""");
        await Sync();

        AccountAchievement slayer = await Db.AccountAchievements.SingleAsync();
        Assert.Equal((1, 1000, true), (slayer.AchievementId, slayer.Current, slayer.Done));
    }

    [Fact]
    public async Task Achievements_WithoutThePermission_AreSkipped()
    {
        AccountStateBuilder state = SmallAccount(); // no "progression" permission
        state.Apply(_stub);
        _stub.SetJson("/v2/account/achievements", """[{"id": 1, "current": 120, "max": 1000, "done": false}]""");

        await Sync();

        Assert.Equal(0, await Db.AccountAchievements.CountAsync());
    }

    [Fact]
    public async Task LearnedRecipes_AreStored_AndALaterSyncReplacesThem()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("unlocks");
        state.Apply(_stub);
        _stub.SetJson("/v2/account/recipes", "[7, 12, 12]");
        await Sync();

        Assert.Equal([7, 12], await Db.AccountRecipes.Where(r => r.AccountId == state.Id)
            .OrderBy(r => r.RecipeId).Select(r => r.RecipeId).ToListAsync());

        _stub.SetJson("/v2/account/recipes", "[12, 30]");
        await Sync();

        Assert.Equal([12, 30], await Db.AccountRecipes.OrderBy(r => r.RecipeId).Select(r => r.RecipeId).ToListAsync());
    }

    [Fact]
    public async Task LearnedRecipes_WithoutThePermission_AreSkipped()
    {
        AccountStateBuilder state = SmallAccount(); // no "unlocks" permission
        state.Apply(_stub);
        _stub.SetJson("/v2/account/recipes", "[7]");

        await Sync();

        Assert.Equal(0, await Db.AccountRecipes.CountAsync());
    }

    [Fact]
    public async Task WardrobeUnlocks_AreStoredByKind_AndALaterSyncReplacesThem()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("unlocks");
        state.Apply(_stub);
        _stub.SetJson("/v2/account/dyes", "[1, 2]");
        _stub.SetJson("/v2/account/minis", "[5]");
        _stub.SetJson("/v2/account/skins", "[10, 11, 12]");
        await Sync();

        Assert.Equal(
            [("Dye", 1), ("Dye", 2), ("Mini", 5), ("Skin", 10), ("Skin", 11), ("Skin", 12)],
            (await Db.AccountUnlocks.Where(u => u.AccountId == state.Id).ToListAsync())
                .Select(u => (u.Kind, u.UnlockId)).Order().ToList());

        _stub.SetJson("/v2/account/minis", "[5, 6]");
        await Sync();

        Assert.Equal([5, 6], await Db.AccountUnlocks.Where(u => u.Kind == AccountUnlockKind.Mini)
            .OrderBy(u => u.UnlockId).Select(u => u.UnlockId).ToListAsync());
    }

    [Fact]
    public async Task Unlocks_OfEveryKind_AndTheLegendaryArmory_AreStored()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("unlocks");
        state.Apply(_stub);
        _stub.SetJson("/v2/account/novelties", "[3]");
        _stub.SetJson("/v2/account/outfits", "[4]");
        // A finisher with charges left isn't owned for good.
        _stub.SetJson("/v2/account/finishers",
            """[{"id": 7, "permanent": true}, {"id": 8, "permanent": false, "quantity": 5}]""");
        _stub.SetJson("/v2/account/mailcarriers", "[9]");
        _stub.SetJson("/v2/account/gliders", "[13]");
        _stub.SetJson("/v2/account/jadebots", "[14]");
        _stub.SetJson("/v2/account/legendaryarmory", """[{"id": 30684, "count": 2}, {"id": 80111, "count": 1}]""");
        await Sync();

        Assert.Equal(
            [("Finisher", 7), ("Glider", 13), ("JadeBot", 14), ("MailCarrier", 9), ("Novelty", 3), ("Outfit", 4)],
            (await Db.AccountUnlocks.ToListAsync()).Select(u => (u.Kind, u.UnlockId)).Order().ToList());
        Assert.Equal([(30684, 2), (80111, 1)],
            (await Db.AccountLegendaryArmory.ToListAsync()).Select(l => (l.ItemId, l.Count)).Order().ToList());
    }

    [Fact]
    public async Task EquippedItems_AreStoredOnce_WithTheirUpgradesAndInfusions()
    {
        AccountStateBuilder state = SmallAccount();
        // A weapon in two equipment tabs, with a sigil and an infusion; and a legendary from the armory.
        state.Equipment["Alice Necro"] = """
            [{"id": 1000, "slot": "WeaponA1", "upgrades": [24615], "infusions": [49424], "location": "Equipped",
              "tabs": [1, 2]},
             {"id": 30684, "slot": "WeaponB1", "location": "EquippedFromLegendaryArmory", "tabs": [1]}]
            """;
        state.Apply(_stub);
        await Sync();

        Assert.Equal(
            [(1000, "Item", "Equipped"), (24615, "Upgrade", "Equipped"), (30684, "Item", "EquippedFromLegendaryArmory"),
             (49424, "Infusion", "Equipped")],
            (await Db.CharacterEquippedItems.ToListAsync()).Select(e => (e.ItemId, e.Part, e.Location)).Order()
                .ToList());
    }

    [Fact]
    public async Task WardrobeUnlocks_WithoutThePermission_AreSkipped()
    {
        AccountStateBuilder state = SmallAccount(); // no "unlocks" permission
        state.Apply(_stub);
        _stub.SetJson("/v2/account/dyes", "[1]");

        await Sync();

        Assert.Equal(0, await Db.AccountUnlocks.CountAsync());
    }

    /// <summary>The reset endpoints as the API returns them: the master lists, and the account's progress.</summary>
    private void StubResetProgress(string worldBossesDone, string dailyObjectives)
    {
        _stub.SetJson("/v2/worldbosses", """["admiral_taidha_covington", "tequatl_the_sunless"]""");
        _stub.SetJson("/v2/account/worldbosses", worldBossesDone);
        _stub.SetJson("/v2/mapchests", """["verdant_brink_heros_choice_chest"]""");
        _stub.SetJson("/v2/account/mapchests", "[]");
        _stub.SetJson("/v2/dailycrafting", """["lump_of_mithrilium"]""");
        _stub.SetJson("/v2/account/dailycrafting", """["lump_of_mithrilium"]""");
        _stub.SetJson(
            "/v2/raids?ids=all",
            """
            [{"id": "forsaken_thicket", "wings": [{"id": "spirit_vale", "events": [
              {"id": "vale_guardian", "type": "Boss"}, {"id": "spirit_woods", "type": "Checkpoint"}]}]}]
            """
        );
        _stub.SetJson("/v2/account/raids", """["vale_guardian"]""");
        _stub.SetJson(
            "/v2/dungeons?ids=all",
            """
            [{"id": "ascalonian_catacombs", "paths": [{"id": "ac_story", "type": "Story"},
              {"id": "hodgins", "type": "Explorable"}]}]
            """
        );
        _stub.SetJson("/v2/account/dungeons", """["hodgins"]""");
        _stub.SetJson(
            "/v2/account/wizardsvault/daily",
            $$"""
            {"meta_progress_current": 1, "meta_progress_complete": 4, "meta_reward_item_id": 99961,
             "meta_reward_astral": 20, "meta_reward_claimed": false, "objectives": {{dailyObjectives}}}
            """
        );
        _stub.SetJson(
            "/v2/account/wizardsvault/weekly",
            """
            {"meta_progress_current": 0, "meta_progress_complete": 6, "meta_reward_item_id": 99962,
             "meta_reward_astral": 50, "meta_reward_claimed": false, "objectives": []}
            """
        );
        _stub.SetJson(
            "/v2/account/wizardsvault/special",
            """
            {"objectives": [{"id": 300, "title": "Log in", "track": "PvE", "acclaim": 5,
              "progress_current": 0, "progress_complete": 1, "claimed": false}]}
            """
        );
    }

    [Fact]
    public async Task ResetProgress_StoresEveryActivityWithWhetherItsDone_AndTheWizardsVault()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        StubResetProgress(
            """["tequatl_the_sunless"]""",
            """
            [{"id": 1, "title": "Complete 3 Events", "track": "PvE", "acclaim": 10,
              "progress_current": 3, "progress_complete": 3, "claimed": true}]
            """
        );
        await Sync();

        List<AccountResetActivity> bosses = await Db.AccountResetActivities
            .Where(a => a.Kind == ResetActivityKind.WorldBoss).OrderBy(a => a.Order).ToListAsync();
        Assert.Equal(
            [("admiral_taidha_covington", false), ("tequatl_the_sunless", true)],
            bosses.Select(b => (b.ActivityId, b.Done))
        );
        Assert.False((await Db.AccountResetActivities.SingleAsync(a => a.Kind == ResetActivityKind.MapChest)).Done);
        Assert.True((await Db.AccountResetActivities.SingleAsync(a => a.Kind == ResetActivityKind.DailyCrafting)).Done);
        List<AccountResetActivity> raid = await Db.AccountResetActivities
            .Where(a => a.Kind == ResetActivityKind.Raid).OrderBy(a => a.Order).ToListAsync();
        Assert.Equal(
            [("vale_guardian", "spirit_vale", false, true), ("spirit_woods", "spirit_vale", true, false)],
            raid.Select(e => (e.ActivityId, e.Group, e.IsCheckpoint, e.Done))
        );

        List<AccountResetActivity> dungeon = await Db.AccountResetActivities
            .Where(a => a.Kind == ResetActivityKind.Dungeon).OrderBy(a => a.Order).ToListAsync();
        Assert.Equal(
            [("ac_story", "ascalonian_catacombs", false), ("hodgins", "ascalonian_catacombs", true)],
            dungeon.Select(p => (p.ActivityId, p.Group, p.Done))
        );

        AccountWizardsVaultMeta daily = await Db.AccountWizardsVaultMetas.SingleAsync(m => m.Period == "Daily");
        Assert.Equal((1, 4, 20, false), (daily.ProgressCurrent, daily.ProgressComplete, daily.RewardAstral,
            daily.RewardClaimed));
        AccountWizardsVaultObjective events =
            await Db.AccountWizardsVaultObjectives.SingleAsync(o => o.Period == "Daily");
        Assert.Equal(("Complete 3 Events", 10, 3, 3, true), (events.Title, events.Acclaim, events.ProgressCurrent,
            events.ProgressComplete, events.Claimed));
        Assert.Equal(300, (await Db.AccountWizardsVaultObjectives.SingleAsync(o => o.Period == "Special")).ObjectiveId);
        Assert.Equal(2, await Db.AccountWizardsVaultMetas.CountAsync()); // the special objectives have no meta
    }

    [Fact]
    public async Task ResetProgress_ALaterSyncReplacesIt()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        StubResetProgress(
            """["tequatl_the_sunless"]""",
            """
            [{"id": 1, "title": "Complete 3 Events", "track": "PvE", "acclaim": 10,
              "progress_current": 3, "progress_complete": 3, "claimed": true}]
            """
        );
        await Sync();

        // After the reset: nothing done yet, and a new daily objective.
        StubResetProgress(
            "[]",
            """
            [{"id": 2, "title": "Gather 20 resources", "track": "PvE", "acclaim": 10,
              "progress_current": 0, "progress_complete": 20, "claimed": false}]
            """
        );
        await Sync();

        Assert.False(await Db.AccountResetActivities.AnyAsync(a => a.Kind == ResetActivityKind.WorldBoss && a.Done));
        Assert.Equal(2, (await Db.AccountWizardsVaultObjectives.SingleAsync(o => o.Period == "Daily")).ObjectiveId);
    }

    [Fact]
    public async Task ResetProgress_KeepsAKindsLastStateWhenItsRequestFails()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        StubResetProgress("""["tequatl_the_sunless"]""", "[]");
        await Sync();

        _stub.SetStatus("/v2/account/worldbosses", System.Net.HttpStatusCode.NotFound);
        await Sync();

        Assert.True(await Db.AccountResetActivities.AnyAsync(a => a.ActivityId == "tequatl_the_sunless" && a.Done));
    }

    [Fact]
    public async Task ResetProgress_WithoutThePermission_IsSkipped()
    {
        AccountStateBuilder state = SmallAccount(); // no "progression" permission
        state.Apply(_stub);
        StubResetProgress("""["tequatl_the_sunless"]""", "[]");

        await Sync();

        Assert.Equal(0, await Db.AccountResetActivities.CountAsync());
        Assert.Equal(0, await Db.AccountWizardsVaultMetas.CountAsync());
    }

    private static string DailyObjective(int id, int current, bool claimed) =>
        $$"""
        {"id": {{id}}, "title": "Objective {{id}}", "track": "PvE", "acclaim": 10,
         "progress_current": {{current}}, "progress_complete": 3, "claimed": {{(claimed ? "true" : "false")}}}
        """;

    // Untracked: the read context would otherwise hand back the row as it first read it.
    private async Task<DateTimeOffset> DailyCurrentSince() =>
        (await Db.AccountWizardsVaultMetas.AsNoTracking().SingleAsync(m => m.Period == "Daily")).CurrentSinceUtc;

    [Fact]
    public async Task WizardsVault_KeepsWhenASetFirstAppeared_UntilANewSetComes()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        StubResetProgress("[]", $"[{DailyObjective(1, 1, false)}]");
        await Sync();
        DateTimeOffset first = await DailyCurrentSince();

        // The same set, further along (as the API keeps returning it until the account logs in after the reset).
        _time.Advance(TimeSpan.FromHours(5));
        StubResetProgress("[]", $"[{DailyObjective(1, 3, true)}]");
        await Sync();
        Assert.Equal(first, await DailyCurrentSince());

        // A new set: different objectives.
        _time.Advance(TimeSpan.FromHours(1));
        StubResetProgress("[]", $"[{DailyObjective(2, 0, false)}]");
        await Sync();
        Assert.Equal(_time.GetUtcNow(), await DailyCurrentSince());
    }

    [Fact]
    public async Task WizardsVault_ASetWhoseProgressWentBackDownIsNew()
    {
        AccountStateBuilder state = SmallAccount();
        state.Permissions.Add("progression");
        state.Apply(_stub);
        StubResetProgress("[]", $"[{DailyObjective(1, 3, true)}]");
        await Sync();

        // The same objective again, unclaimed and back at 0: a new day's set that repeats yesterday's.
        _time.Advance(TimeSpan.FromHours(20));
        StubResetProgress("[]", $"[{DailyObjective(1, 0, false)}]");
        await Sync();

        Assert.Equal(_time.GetUtcNow(), await DailyCurrentSince());
    }
}
