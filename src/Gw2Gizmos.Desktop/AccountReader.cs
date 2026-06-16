using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Currencies;
using Gw2Gizmos.Data.EntityFramework.Entities.Materials;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Converters;
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
    // GW2's Coin currency id, singled out so session deltas show gold/silver/copper rather than a raw count.
    private const int CoinCurrencyId = 1;

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

            Account? account = db.Accounts.AsNoTracking()
                .OrderByDescending(a => a.LastSyncedUtc)
                .FirstOrDefault();
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

            return db.Accounts.AsNoTracking()
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

    /// <summary>The account's character names from the synced character details, most recently played first.</summary>
    public List<string> GetCharacterNames(string accountId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        return db.Characters.AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .OrderByDescending(c => c.LastModified)
            .Select(c => c.Name)
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

    /// <summary>The account's play sessions, most recent first, each with the characters played that sitting.</summary>
    public List<GameSessionRow> GetGameSessions(string accountId) =>
        SafeRead(db => GetGameSessions(db, accountId), new List<GameSessionRow>());

    private static List<GameSessionRow> GetGameSessions(Gw2GizmosDbContext db, string accountId)
    {
        List<GameSession> sessions = db.GameSessions.AsNoTracking()
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToList();
        if (sessions.Count == 0)
        {
            return new List<GameSessionRow>();
        }

        long[] sessionIds = sessions.Select(s => s.Id).ToArray();
        var segments = db.CharacterSegments.AsNoTracking()
            .Where(s => sessionIds.Contains(s.GameSessionId))
            .OrderBy(s => s.Sequence)
            .Select(s => new { s.GameSessionId, s.CharacterName })
            .ToList();
        Dictionary<long, List<string>> charactersBySession = segments
            .GroupBy(s => s.GameSessionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CharacterName).Distinct().ToList());

        return sessions
            .Select(s => new GameSessionRow(
                s.Id,
                s.StartedAtUtc,
                s.EndedAtUtc,
                charactersBySession.TryGetValue(s.Id, out List<string>? chars) ? string.Join(", ", chars) : "—"))
            .ToList();
    }

    /// <summary>One session's character segments in order, each with its reconstructed coin/item delta.</summary>
    public List<SessionSegmentRow> GetSessionSegments(string accountId, long gameSessionId) =>
        SafeRead(db => GetSessionSegments(db, accountId, gameSessionId), new List<SessionSegmentRow>());

    private static List<SessionSegmentRow> GetSessionSegments(Gw2GizmosDbContext db, string accountId, long gameSessionId)
    {
        bool ownsSession = db.GameSessions.AsNoTracking().Any(s => s.Id == gameSessionId && s.AccountId == accountId);
        if (!ownsSession)
        {
            return new List<SessionSegmentRow>();
        }

        List<CharacterSegment> segments = db.CharacterSegments.AsNoTracking()
            .Where(s => s.GameSessionId == gameSessionId)
            .OrderBy(s => s.Sequence)
            .ToList();

        var rows = new List<SessionSegmentRow>();
        foreach (CharacterSegment segment in segments)
        {
            DateTimeOffset end = segment.EndedAtUtc ?? DateTimeOffset.UtcNow;
            Dictionary<int, int> startItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, segment.StartedAtUtc);
            Dictionary<int, int> endItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, end);

            int gained = 0;
            int lost = 0;
            foreach (int itemId in startItems.Keys.Union(endItems.Keys))
            {
                int delta = endItems.GetValueOrDefault(itemId) - startItems.GetValueOrDefault(itemId);
                if (delta > 0)
                {
                    gained += delta;
                }
                else if (delta < 0)
                {
                    lost += -delta;
                }
            }

            long coinDelta = AccountHoldingsReconstructor.WalletValueAsOf(db, accountId, CoinCurrencyId, end)
                - AccountHoldingsReconstructor.WalletValueAsOf(db, accountId, CoinCurrencyId, segment.StartedAtUtc);

            rows.Add(new SessionSegmentRow(
                segment.Id, segment.Sequence, segment.CharacterName,
                segment.StartedAtUtc, segment.EndedAtUtc, coinDelta, gained, lost));
        }

        return rows;
    }

    /// <summary>One segment's full "hoarded" delta — currencies and items gained/lost over its time window.</summary>
    public SessionLoot GetSegmentLoot(string accountId, long segmentId) =>
        SafeRead(
            db => GetSegmentLoot(db, accountId, segmentId),
            new SessionLoot(Array.Empty<SessionLootCurrency>(), Array.Empty<SessionLootItem>()));

    private SessionLoot GetSegmentLoot(Gw2GizmosDbContext db, string accountId, long segmentId)
    {
        CharacterSegment? segment = db.CharacterSegments.AsNoTracking().FirstOrDefault(s => s.Id == segmentId);
        return segment is null
            ? new SessionLoot(Array.Empty<SessionLootCurrency>(), Array.Empty<SessionLootItem>())
            : ComputeLoot(db, accountId, segment.StartedAtUtc, segment.EndedAtUtc ?? DateTimeOffset.UtcNow);
    }

    /// <summary>The whole session's aggregated "hoarded" delta — currencies and items gained/lost across the sitting.</summary>
    public SessionLoot GetGameSessionLoot(string accountId, long gameSessionId) =>
        SafeRead(
            db =>
            {
                GameSession? session = db.GameSessions.AsNoTracking()
                    .FirstOrDefault(s => s.Id == gameSessionId && s.AccountId == accountId);
                return session is null
                    ? new SessionLoot(Array.Empty<SessionLootCurrency>(), Array.Empty<SessionLootItem>())
                    : ComputeLoot(db, accountId, session.StartedAtUtc, session.EndedAtUtc ?? DateTimeOffset.UtcNow);
            },
            new SessionLoot(Array.Empty<SessionLootCurrency>(), Array.Empty<SessionLootItem>()));

    /// <summary>Reconstructs the currencies and items gained/lost between two instants, with names/icons resolved.</summary>
    private SessionLoot ComputeLoot(Gw2GizmosDbContext db, string accountId, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        // Item deltas across all containers.
        Dictionary<int, int> startItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, startUtc);
        Dictionary<int, int> endItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, endUtc);
        var itemDeltas = startItems.Keys.Union(endItems.Keys)
            .Select(id => (Id: id, Delta: endItems.GetValueOrDefault(id) - startItems.GetValueOrDefault(id)))
            .Where(x => x.Delta != 0)
            .ToList();
        Dictionary<int, string> itemNames = LoadNames(itemDeltas.Select(x => x.Id), (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));
        List<SessionLootItem> items = itemDeltas
            .OrderByDescending(x => Math.Abs(x.Delta))
            .Select(x => new SessionLootItem(x.Id, itemNames.GetValueOrDefault(x.Id) ?? $"Item {x.Id}", x.Delta))
            .ToList();

        // Currency deltas.
        Dictionary<int, long> startWallet = AccountHoldingsReconstructor.WalletTotalsAsOf(db, accountId, startUtc);
        Dictionary<int, long> endWallet = AccountHoldingsReconstructor.WalletTotalsAsOf(db, accountId, endUtc);
        var currencyDeltas = startWallet.Keys.Union(endWallet.Keys)
            .Select(id => (Id: id, Delta: endWallet.GetValueOrDefault(id) - startWallet.GetValueOrDefault(id)))
            .Where(x => x.Delta != 0)
            .ToList();
        int[] currencyIds = currencyDeltas.Select(x => x.Id).ToArray();
        Dictionary<int, Currency> currencies = db.Currencies.AsNoTracking()
            .Where(c => currencyIds.Contains(c.Id))
            .ToDictionary(c => c.Id);
        List<SessionLootCurrency> currencyRows = currencyDeltas
            .OrderBy(x => currencies.TryGetValue(x.Id, out Currency? c) ? c.Order : int.MaxValue)
            .Select(x =>
            {
                currencies.TryGetValue(x.Id, out Currency? c);
                return new SessionLootCurrency(x.Id, c?.Name ?? $"Currency {x.Id}", c?.Icon ?? "", x.Delta);
            })
            .ToList();

        return new SessionLoot(currencyRows, items);
    }

    /// <summary>The number of characters synced for an account (for the dashboard).</summary>
    public int GetCharacterCount(string accountId) =>
        SafeRead(db => db.Characters.AsNoTracking().Count(c => c.AccountId == accountId), 0);

    /// <summary>Row counts for each deletable data category of an account (for the Stored data section).</summary>
    public Dictionary<string, int> GetAccountDataCounts(string accountId) =>
        SafeRead(
            db => new Dictionary<string, int>
            {
                [DeletableData.Wallet] = db.AccountWalletObservations.Count(o => o.AccountId == accountId),
                [DeletableData.Materials] = db.AccountItemObservations
                    .Count(o => o.AccountId == accountId && o.Container == AccountContainer.MaterialStorage),
                [DeletableData.Bank] = db.AccountItemObservations
                    .Count(o => o.AccountId == accountId && o.Container == AccountContainer.Bank)
                    + db.AccountContainerSlots.Count(s => s.AccountId == accountId && s.Store == AccountContainer.Bank),
                [DeletableData.SharedInventory] = db.AccountItemObservations
                    .Count(o => o.AccountId == accountId && o.Container == AccountContainer.SharedInventory)
                    + db.AccountContainerSlots.Count(s => s.AccountId == accountId && s.Store == AccountContainer.SharedInventory),
                [DeletableData.Characters] = db.AccountItemObservations
                    .Count(o => o.AccountId == accountId && o.Container == AccountContainer.CharacterInventory)
                    + db.CharacterItemSlots.Count(s => s.AccountId == accountId)
                    + db.Characters.Count(c => c.AccountId == accountId),
                [DeletableData.Sessions] = db.GameSessions.Count(g => g.AccountId == accountId)
                    + db.CharacterSegments.Count(seg =>
                        db.GameSessions.Any(g => g.Id == seg.GameSessionId && g.AccountId == accountId)),
            },
            new Dictionary<string, int>());

    /// <summary>The number of stored trading-post price points (global, not per-account).</summary>
    public int GetPriceHistoryCount() =>
        SafeRead(db => db.PriceSnapshots.Count(), 0);

    /// <summary>The account's current coin balance (currency 1), in copper.</summary>
    public long GetCoinBalance(string accountId) =>
        SafeRead(
            db => db.AccountWalletObservations.AsNoTracking()
                .Where(o => o.AccountId == accountId && o.CurrencyId == CoinCurrencyId)
                .OrderByDescending(o => o.Id)
                .Select(o => o.Value)
                .FirstOrDefault(),
            0L);

    /// <summary>Aggregate play-session stats for the dashboard: count, total play time, and the current/last character.</summary>
    public DashboardSessionStats GetSessionStats(string accountId) =>
        SafeRead(
            db =>
            {
                var sessions = db.GameSessions.AsNoTracking()
                    .Where(s => s.AccountId == accountId)
                    .Select(s => new { s.Id, s.StartedAtUtc, s.EndedAtUtc })
                    .ToList();
                if (sessions.Count == 0)
                {
                    return new DashboardSessionStats(0, TimeSpan.Zero, null, false, null);
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                TimeSpan total = TimeSpan.Zero;
                foreach (var s in sessions)
                {
                    DateTimeOffset end = s.EndedAtUtc ?? now;
                    if (end > s.StartedAtUtc)
                    {
                        total += end - s.StartedAtUtc;
                    }
                }

                // Most recent sitting (the list is already materialised for the playtime sum above).
                var latest = sessions.OrderByDescending(s => s.StartedAtUtc).First();
                bool isPlaying = latest.EndedAtUtc is null;
                string? character = db.CharacterSegments.AsNoTracking()
                    .Where(seg => seg.GameSessionId == latest.Id)
                    .OrderByDescending(seg => seg.Sequence)
                    .Select(seg => seg.CharacterName)
                    .FirstOrDefault();

                return new DashboardSessionStats(
                    sessions.Count, total, character, isPlaying, latest.EndedAtUtc ?? latest.StartedAtUtc);
            },
            new DashboardSessionStats(0, TimeSpan.Zero, null, false, null));

    // Runs a read in a fresh scope, returning the fallback on any error — notably "no such table" during the brief
    // window after an upgrade where the desktop has the new code but the worker hasn't applied the migration yet.
    private T SafeRead<T>(Func<Gw2GizmosDbContext, T> read, T fallback)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            return read(db);
        }
        catch (Exception)
        {
            return fallback;
        }
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

/// <summary>Aggregate play-session stats for the dashboard.</summary>
public sealed record DashboardSessionStats(
    int Count,
    TimeSpan TotalPlaytime,
    string? Character,
    bool IsPlaying,
    DateTimeOffset? LastPlayedUtc
);

/// <summary>One play session for the Sessions hub: when it ran and which characters were played.</summary>
public sealed record GameSessionRow(long Id, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, string CharactersDisplay)
{
    public bool IsActive => EndedAtUtc is null;

    /// <summary>Card title, e.g. "Mon 16 Jun, 19:04".</summary>
    public string Title => StartedAtUtc.LocalDateTime.ToString("ddd d MMM, HH:mm");

    public string Subtitle => $"{DurationDisplay} · {CharactersDisplay}";

    public string DurationDisplay => SessionFormat.Duration(StartedAtUtc, EndedAtUtc);
}

/// <summary>One character segment within a session, with its reconstructed coin/item delta.</summary>
public sealed record SessionSegmentRow(
    long Id,
    int Sequence,
    string CharacterName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    long CoinDelta,
    int ItemsGained,
    int ItemsLost
)
{
    public bool IsActive => EndedAtUtc is null;

    /// <summary>Local time range, e.g. "19:04 – 19:48" (or "19:04 – now" while active).</summary>
    public string TimeRange =>
        $"{StartedAtUtc.LocalDateTime:HH:mm} – {(EndedAtUtc is { } e ? e.LocalDateTime.ToString("HH:mm") : "now")}";

    public string DurationDisplay => SessionFormat.Duration(StartedAtUtc, EndedAtUtc);

    public bool HasCoin => CoinDelta != 0;

    public string CoinDisplay => SessionFormat.SignedCoin(CoinDelta);

    public bool HasItems => ItemsGained != 0 || ItemsLost != 0;

    /// <summary>Compact item summary, e.g. "+128 / −12 items".</summary>
    public string ItemsDisplay
    {
        get
        {
            var parts = new List<string>();
            if (ItemsGained != 0)
            {
                parts.Add($"+{ItemsGained:N0}");
            }

            if (ItemsLost != 0)
            {
                parts.Add($"−{ItemsLost:N0}");
            }

            return parts.Count == 0 ? "no item change" : string.Join(" / ", parts) + " items";
        }
    }
}

/// <summary>A currency gained/lost over a segment (coin shown as g/s/c, others as a signed count).</summary>
public sealed record SessionLootCurrency(int CurrencyId, string Name, string IconUrl, long Delta)
{
    public string DeltaDisplay => CurrencyId == 1
        ? SessionFormat.SignedCoin(Delta)
        : (Delta >= 0 ? "+" : "−") + Math.Abs(Delta).ToString("N0");
}

/// <summary>An item gained/lost over a segment.</summary>
public sealed record SessionLootItem(int ItemId, string Name, int Delta)
{
    public string DeltaDisplay => (Delta >= 0 ? "+" : "−") + Math.Abs(Delta).ToString("N0");
}

/// <summary>A segment's full hoarded delta: currencies and items gained/lost over its window.</summary>
public sealed record SessionLoot(IReadOnlyList<SessionLootCurrency> Currencies, IReadOnlyList<SessionLootItem> Items)
{
    public bool HasCurrencies => Currencies.Count > 0;
    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => Currencies.Count == 0 && Items.Count == 0;
}

/// <summary>Shared formatting for the session views (durations and signed coin amounts).</summary>
internal static class SessionFormat
{
    public static string Duration(DateTimeOffset start, DateTimeOffset? end)
    {
        TimeSpan span = (end ?? DateTimeOffset.UtcNow) - start;
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes}m"
            : span.TotalMinutes >= 1
                ? $"{span.Minutes}m"
                : $"{span.Seconds}s";
    }

    public static string SignedCoin(long copper) =>
        (copper >= 0 ? "+" : "−") + Coin.Format(Math.Abs(copper));
}
