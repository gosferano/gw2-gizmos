using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Currencies;
using Gw2Gizmos.Data.EntityFramework.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Read-only access to the worker-owned account data for the desktop view-models. Each call opens its own
/// short scope and queries the current (most-recently-synced) account, so navigating to a section always
/// reflects the latest sync. Heavy reads are meant to run off the UI thread (the view-models wrap them in
/// <c>Task.Run</c>).
/// </summary>
public sealed class AccountReader
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AccountReader(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The current account (the API key's), or null if nothing has synced yet.</summary>
    public AccountInfo? GetCurrentAccount()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            // SQLite can't ORDER BY a DateTimeOffset; there are only ever a few accounts, so pick in memory.
            Account? account = db.Accounts.AsNoTracking().AsEnumerable().MaxBy(a => a.LastSyncedUtc);
            return account is null
                ? null
                : new AccountInfo(account.AccountId, account.Name, account.World, account.LastSyncedUtc);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>All synced accounts (one per API key), most-recently-synced first, for the Account list.</summary>
    public List<AccountInfo> GetAccounts()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            return db.Accounts.AsNoTracking().AsEnumerable()
                .OrderByDescending(a => a.LastSyncedUtc)
                .Select(a => new AccountInfo(a.AccountId, a.Name, a.World, a.LastSyncedUtc))
                .ToList();
        }
        catch (Exception)
        {
            return new List<AccountInfo>();
        }
    }

    public List<WalletRow> GetWallet(string accountId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        IQueryable<long> maxIds = db.AccountWalletObservations
            .Where(o => o.AccountId == accountId)
            .GroupBy(o => o.CurrencyId)
            .Select(g => g.Max(x => x.Id));
        List<AccountWalletObservation> latest = db.AccountWalletObservations.AsNoTracking()
            .Where(o => maxIds.Contains(o.Id) && o.Value > 0)
            .ToList();

        // Pull the currencies' display data (name/icon/order); a wallet holds ~60, well under the IN limit.
        int[] ids = latest.Select(o => o.CurrencyId).ToArray();
        Dictionary<int, Currency> currencies = db.Currencies.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionary(c => c.Id);

        return latest
            .OrderBy(o => currencies.TryGetValue(o.CurrencyId, out Currency? c) ? c.Order : int.MaxValue)
            .Select(o =>
            {
                currencies.TryGetValue(o.CurrencyId, out Currency? c);
                return new WalletRow(c?.Name ?? $"Currency {o.CurrencyId}", o.Value, c?.Icon ?? "");
            })
            .ToList();
    }

    public List<MaterialCategoryView> GetMaterials(string accountId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        List<MaterialCategory> categories = db.MaterialCategories.AsNoTracking().OrderBy(c => c.Order).ToList();
        Dictionary<int, List<MaterialCategoryItem>> itemsByCategory = db.MaterialCategoryItems.AsNoTracking()
            .OrderBy(ci => ci.Position)
            .ToList()
            .GroupBy(ci => ci.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        IQueryable<long> maxIds = db.AccountItemObservations
            .Where(o => o.AccountId == accountId && o.Container == AccountContainer.MaterialStorage)
            .GroupBy(o => o.ItemId)
            .Select(g => g.Max(x => x.Id));
        Dictionary<int, int> counts = db.AccountItemObservations.AsNoTracking()
            .Where(o => maxIds.Contains(o.Id))
            .ToDictionary(o => o.ItemId, o => o.Count);

        int[] allItemIds = itemsByCategory.Values.SelectMany(v => v.Select(ci => ci.ItemId)).Distinct().ToArray();
        Dictionary<int, string> names = LoadNames(allItemIds, (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));

        var views = new List<MaterialCategoryView>();
        foreach (MaterialCategory category in categories)
        {
            if (!itemsByCategory.TryGetValue(category.Id, out List<MaterialCategoryItem>? items))
            {
                continue;
            }

            List<SlotRow> rows = items
                .Select(ci => new SlotRow(ci.ItemId, names.GetValueOrDefault(ci.ItemId) ?? $"Item {ci.ItemId}", counts.GetValueOrDefault(ci.ItemId)))
                .ToList();
            views.Add(new MaterialCategoryView(category.Name, rows));
        }

        return views;
    }

    public List<SlotRow> GetSlots(string accountId, string store)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        List<AccountContainerSlot> slots = db.AccountContainerSlots.AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Store == store)
            .OrderBy(s => s.SlotIndex)
            .ToList();

        Dictionary<int, string> names = LoadNames(slots.Where(s => s.ItemId.HasValue).Select(s => s.ItemId!.Value), (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));

        return slots
            .Select(s => s.ItemId is int itemId
                ? new SlotRow(itemId, names.GetValueOrDefault(itemId) ?? $"Item {itemId}", s.Count)
                : SlotRow.Empty)
            .ToList();
    }

    /// <summary>The account's character names from the synced character details, alphabetical.</summary>
    public List<string> GetCharacterNames(string accountId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        return db.Characters.AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .Select(c => c.Name)
            .OrderBy(name => name)
            .ToList();
    }

    /// <summary>One character's current bag layout (slot order, empties included) for the inventory grid.</summary>
    public List<SlotRow> GetCharacterInventory(string accountId, string characterName)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        List<CharacterItemSlot> slots = db.CharacterItemSlots.AsNoTracking()
            .Where(s => s.AccountId == accountId && s.CharacterName == characterName)
            .OrderBy(s => s.SlotIndex)
            .ToList();

        Dictionary<int, string> names = LoadNames(slots.Where(s => s.ItemId.HasValue).Select(s => s.ItemId!.Value), (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));

        return slots
            .Select(s => s.ItemId is int itemId
                ? new SlotRow(itemId, names.GetValueOrDefault(itemId) ?? $"Item {itemId}", s.Count)
                : SlotRow.Empty)
            .ToList();
    }

    /// <summary>
    /// The account's total count of each item across every location (material storage, bank, shared inventory,
    /// character bags) from the latest observation per (location, item). Backs the unified-inventory / play-session
    /// "hoarded" views.
    /// </summary>
    public Dictionary<int, int> GetUnifiedItemTotals(string accountId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        // Latest row per (container, item); sum the current counts across containers into a per-item total.
        IQueryable<long> maxIds = db.AccountItemObservations
            .Where(o => o.AccountId == accountId)
            .GroupBy(o => new { o.Container, o.ItemId })
            .Select(g => g.Max(x => x.Id));

        var totals = new Dictionary<int, int>();
        foreach (var row in db.AccountItemObservations.AsNoTracking()
                     .Where(o => maxIds.Contains(o.Id) && o.Count > 0)
                     .Select(o => new { o.ItemId, o.Count }))
        {
            totals[row.ItemId] = totals.GetValueOrDefault(row.ItemId) + row.Count;
        }

        return totals;
    }

    // Resolves id→name in batches of 500 to stay under SQLite's ~999 bound-parameter limit.
    private Dictionary<int, string> LoadNames(
        IEnumerable<int> ids,
        Func<Gw2GizmosDbContext, int[], IEnumerable<IdName>> query
    )
    {
        var names = new Dictionary<int, string>();
        int[] distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0)
        {
            return names;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        foreach (int[] batch in distinct.Chunk(500))
        {
            foreach (IdName row in query(db, batch))
            {
                names[row.Id] = row.Name;
            }
        }

        return names;
    }

    private readonly record struct IdName(int Id, string Name);
}

/// <summary>The current account's identity for the Account landing header.</summary>
public sealed record AccountInfo(string Id, string Name, int World, DateTimeOffset LastSyncedUtc)
{
    /// <summary>Card subtitle, e.g. "World 1001 · synced 14/06/2026 04:12".</summary>
    public string Subtitle => $"World {World} · synced {LastSyncedUtc.LocalDateTime:g}";
}
