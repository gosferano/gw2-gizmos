using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Integration tests for <see cref="AccountDataDeleter"/>: each category deletes exactly its own rows for the
/// target account, leaves every other category intact, and never touches a second account's data. Seeds two
/// accounts ("A" and "B") with a row in every table plus global price history, then asserts the scoping.
/// </summary>
public sealed class AccountDataDeleterTests : IDisposable
{
    private const string A = "A.1111";
    private const string B = "B.2222";

    private readonly WorkerDbFixture _fixture = new();

    public AccountDataDeleterTests() => Seed();

    public void Dispose() => _fixture.Dispose();

    private async Task Delete(string typeKey, string? accountId = A)
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        var deleter = new AccountDataDeleter(db, NullLogger<AccountDataDeleter>.Instance);
        await deleter.DeleteAsync(typeKey, accountId, CancellationToken.None);
    }

    [Fact]
    public async Task Wallet_DeletesOnlyWalletForAccount()
    {
        await Delete(DeletableData.Wallet);

        Assert.Equal(0, await Count(d => d.AccountWalletObservations.Count(o => o.AccountId == A)));
        // Other categories for A untouched...
        Assert.True(await Count(d => d.AccountItemObservations.Count(o => o.AccountId == A)) > 0);
        // ...and B's wallet fully intact.
        Assert.Equal(2, await Count(d => d.AccountWalletObservations.Count(o => o.AccountId == B)));
    }

    [Fact]
    public async Task Materials_DeletesOnlyMaterialStorageObservations()
    {
        await Delete(DeletableData.Materials);

        Assert.Equal(0, await ItemObs(A, AccountContainer.MaterialStorage));
        // The other item containers for A survive.
        Assert.True(await ItemObs(A, AccountContainer.Bank) > 0);
        Assert.True(await ItemObs(A, AccountContainer.SharedInventory) > 0);
        Assert.True(await ItemObs(A, AccountContainer.CharacterInventory) > 0);
        Assert.True(await ItemObs(B, AccountContainer.MaterialStorage) > 0);
    }

    [Fact]
    public async Task Bank_DeletesBankObservationsAndSlots()
    {
        await Delete(DeletableData.Bank);

        Assert.Equal(0, await ItemObs(A, AccountContainer.Bank));
        Assert.Equal(0, await Count(d => d.AccountContainerSlots.Count(s => s.AccountId == A && s.Store == AccountContainer.Bank)));
        // Shared-inventory slots for A are a different store — untouched.
        Assert.True(await Count(d => d.AccountContainerSlots.Count(s => s.AccountId == A && s.Store == AccountContainer.SharedInventory)) > 0);
        Assert.True(await ItemObs(B, AccountContainer.Bank) > 0);
    }

    [Fact]
    public async Task SharedInventory_DeletesSharedObservationsAndSlots()
    {
        await Delete(DeletableData.SharedInventory);

        Assert.Equal(0, await ItemObs(A, AccountContainer.SharedInventory));
        Assert.Equal(0, await Count(d => d.AccountContainerSlots.Count(s => s.AccountId == A && s.Store == AccountContainer.SharedInventory)));
        Assert.True(await Count(d => d.AccountContainerSlots.Count(s => s.AccountId == A && s.Store == AccountContainer.Bank)) > 0);
        Assert.True(await ItemObs(B, AccountContainer.SharedInventory) > 0);
    }

    [Fact]
    public async Task Characters_DeletesRosterSlotsAndCharacterObservations()
    {
        await Delete(DeletableData.Characters);

        Assert.Equal(0, await ItemObs(A, AccountContainer.CharacterInventory));
        Assert.Equal(0, await Count(d => d.CharacterItemSlots.Count(s => s.AccountId == A)));
        Assert.Equal(0, await Count(d => d.Characters.Count(c => c.AccountId == A)));
        // B's roster + character data intact.
        Assert.Equal(1, await Count(d => d.Characters.Count(c => c.AccountId == B)));
        Assert.True(await Count(d => d.CharacterItemSlots.Count(s => s.AccountId == B)) > 0);
    }

    [Fact]
    public async Task Sessions_DeletesSessionsAndSegments()
    {
        await Delete(DeletableData.Sessions);

        Assert.Equal(0, await Count(d => d.GameSessions.Count(g => g.AccountId == A)));
        Assert.Equal(0, await Count(d => d.CharacterSegments.Count(s => d.GameSessions.Any(g => g.Id == s.GameSessionId && g.AccountId == A))));
        // B's session + its segments survive.
        Assert.Equal(1, await Count(d => d.GameSessions.Count(g => g.AccountId == B)));
        Assert.True(await Count(d => d.CharacterSegments.Count(s => d.GameSessions.Any(g => g.Id == s.GameSessionId && g.AccountId == B))) > 0);
    }

    [Fact]
    public async Task AllForAccount_WipesEverythingForAccountIncludingIdentity()
    {
        await Delete(DeletableData.AllForAccount);

        // Account A has no rows left anywhere, including the Account identity row.
        Assert.Equal(0, await Count(d => d.Accounts.Count(a => a.AccountId == A)));
        Assert.Equal(0, await Count(d => d.AccountWalletObservations.Count(o => o.AccountId == A)));
        Assert.Equal(0, await Count(d => d.AccountItemObservations.Count(o => o.AccountId == A)));
        Assert.Equal(0, await Count(d => d.AccountContainerSlots.Count(s => s.AccountId == A)));
        Assert.Equal(0, await Count(d => d.CharacterItemSlots.Count(s => s.AccountId == A)));
        Assert.Equal(0, await Count(d => d.Characters.Count(c => c.AccountId == A)));
        Assert.Equal(0, await Count(d => d.GameSessions.Count(g => g.AccountId == A)));

        // Account B is fully intact, and global price history is not account-scoped so it survives.
        Assert.Equal(1, await Count(d => d.Accounts.Count(a => a.AccountId == B)));
        Assert.True(await Count(d => d.AccountItemObservations.Count(o => o.AccountId == B)) > 0);
        Assert.Equal(3, await Count(d => d.PriceSnapshots.Count()));
    }

    [Fact]
    public async Task PriceHistory_DeletesAllSnapshotsAndIsGlobal()
    {
        // Global category — no account id needed.
        await Delete(DeletableData.PriceHistory, accountId: null);

        Assert.Equal(0, await Count(d => d.PriceSnapshots.Count()));
        // Account data is untouched.
        Assert.True(await Count(d => d.AccountItemObservations.Count(o => o.AccountId == A)) > 0);
        Assert.Equal(1, await Count(d => d.Accounts.Count(a => a.AccountId == A)));
    }

    [Fact]
    public async Task AccountScopedDelete_WithNoAccount_IsNoOp()
    {
        int before = await Count(d => d.AccountWalletObservations.Count());

        // Wallet is account-scoped; with no account id the deleter must skip rather than wipe everyone's wallet.
        await Delete(DeletableData.Wallet, accountId: null);

        Assert.Equal(before, await Count(d => d.AccountWalletObservations.Count()));
    }

    // --- assertion helpers (fresh context each time so deletes are observed) ---

    private async Task<int> Count(Func<Gw2GizmosDbContext, int> query)
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        return query(db);
    }

    private Task<int> ItemObs(string accountId, string container) =>
        Count(d => d.AccountItemObservations.Count(o => o.AccountId == accountId && o.Container == container));

    // --- seeding ---

    private void Seed()
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        foreach (string accountId in new[] { A, B })
        {
            SeedAccount(db, accountId);
        }

        // Global price history (not account-scoped).
        for (int i = 0; i < 3; i++)
        {
            db.PriceSnapshots.Add(new PriceSnapshot { ItemId = 1000 + i, TimestampUtc = DateTimeOffset.UnixEpoch, Buy = 1, Sell = 2 });
        }

        db.SaveChanges();
    }

    private static void SeedAccount(Gw2GizmosDbContext db, string accountId)
    {
        DateTimeOffset t = DateTimeOffset.UnixEpoch;

        db.Accounts.Add(new Account { AccountId = accountId, Name = accountId, World = 1001, LastSyncedUtc = t });

        db.AccountWalletObservations.Add(new AccountWalletObservation { AccountId = accountId, CurrencyId = 1, Value = 100, ObservedAtUtc = t });
        db.AccountWalletObservations.Add(new AccountWalletObservation { AccountId = accountId, CurrencyId = 2, Value = 200, ObservedAtUtc = t });

        // One item observation in each container so per-container scoping is testable.
        foreach (string container in new[]
                 {
                     AccountContainer.MaterialStorage, AccountContainer.Bank,
                     AccountContainer.SharedInventory, AccountContainer.CharacterInventory,
                 })
        {
            db.AccountItemObservations.Add(new AccountItemObservation { AccountId = accountId, Container = container, ItemId = 10, Count = 1, ObservedAtUtc = t });
        }

        // Slot grids for the two slot-based stores.
        db.AccountContainerSlots.Add(new AccountContainerSlot { AccountId = accountId, Store = AccountContainer.Bank, SlotIndex = 0, ItemId = 10, Count = 1 });
        db.AccountContainerSlots.Add(new AccountContainerSlot { AccountId = accountId, Store = AccountContainer.SharedInventory, SlotIndex = 0, ItemId = 11, Count = 1 });

        // Character roster + a bag slot.
        db.Characters.Add(new Character { AccountId = accountId, Name = $"{accountId}-Char" });
        db.CharacterItemSlots.Add(new CharacterItemSlot { AccountId = accountId, CharacterName = $"{accountId}-Char", SlotIndex = 0, ItemId = 10, Count = 1 });

        // A play session with two segments.
        var session = new GameSession { AccountId = accountId, StartedAtUtc = t };
        db.GameSessions.Add(session);
        db.SaveChanges(); // assign session.Id
        db.CharacterSegments.Add(new CharacterSegment { GameSessionId = session.Id, Sequence = 0, CharacterName = $"{accountId}-Char", StartedAtUtc = t });
        db.CharacterSegments.Add(new CharacterSegment { GameSessionId = session.Id, Sequence = 1, CharacterName = $"{accountId}-Char", StartedAtUtc = t });
    }
}
