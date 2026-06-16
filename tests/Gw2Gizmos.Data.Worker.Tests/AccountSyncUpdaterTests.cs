using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
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
    }

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
}
