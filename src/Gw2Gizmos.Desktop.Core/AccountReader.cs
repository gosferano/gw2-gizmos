using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Currencies;
using Gw2Gizmos.Data.EntityFramework.Entities.Materials;
using Gw2Gizmos.Data.EntityFramework.Entities.Professions;
using Gw2Gizmos.Data.Worker.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

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

    /// <summary>The account's characters as cards (profession, elite specialization, crafting…), most recently played first.</summary>
    public List<CharacterCard> GetCharacterCards(string accountId) =>
        SafeRead(db => ReadCharacterCards(db, db.Characters.Where(c => c.AccountId == accountId)), new List<CharacterCard>());

    /// <summary>One character's card, or null when it isn't synced.</summary>
    public CharacterCard? GetCharacterCard(string accountId, string characterName) =>
        SafeRead(
            db => ReadCharacterCards(db, db.Characters.Where(c => c.AccountId == accountId && c.Name == characterName))
                .FirstOrDefault(),
            null);

    private static List<CharacterCard> ReadCharacterCards(Gw2GizmosDbContext db, IQueryable<Character> query)
    {
        List<Character> characters = query.AsNoTracking().OrderByDescending(c => c.LastModified).ToList();
        Dictionary<string, Profession> professions = db.Professions.AsNoTracking().ToDictionary(p => p.Id);
        // Only the elite ones matter: a character's elite specialization picks its name and profession icon.
        Dictionary<int, Specialization> elites = db.Specializations.AsNoTracking()
            .Where(s => s.Elite)
            .ToDictionary(s => s.Id);

        return characters
            .Select(c =>
            {
                Specialization? elite = c.Specializations
                    .Select(id => elites.GetValueOrDefault(id))
                    .FirstOrDefault(s => s is not null);
                Profession? profession = professions.GetValueOrDefault(c.Profession);
                return new CharacterCard(
                    c.Name,
                    c.Profession,
                    c.Race,
                    c.Gender,
                    c.Level,
                    c.Age,
                    c.Created,
                    c.Deaths,
                    elite?.ProfessionIconBig ?? profession?.IconBig,
                    elite?.Name,
                    (c.Crafting ?? [])
                        .OrderByDescending(d => d.Active)
                        .ThenByDescending(d => d.Rating)
                        .Select(d => new CharacterCraftingLevel(d.Discipline, d.Rating, d.Active))
                        .ToList());
            })
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
            .Select(s => new { s.Id, s.GameSessionId, s.CharacterName })
            .ToList();
        Dictionary<long, List<string>> charactersBySession = segments
            .GroupBy(s => s.GameSessionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CharacterName).Distinct().ToList());

        // Each sitting's main map: the one it spent the most time on.
        Dictionary<long, long> sessionBySegment = segments.ToDictionary(s => s.Id, s => s.GameSessionId);
        Dictionary<long, string> mainMapBySession = MapTimes(db, sessionBySegment.Keys.ToList())
            .GroupBy(t => sessionBySegment[t.SegmentId])
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.MapId).OrderByDescending(m => m.Sum(t => t.Time.Ticks)).First().First().Name);

        // Reconstruct each sitting's item + coin delta over its window, then value them all with one shared
        // price/vendor lookup so the list badge matches the session-detail summary.
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<long, (Dictionary<int, int> Items, long Coin)> deltas = sessions.ToDictionary(
            s => s.Id,
            s => (
                ItemDeltaOver(db, accountId, s.StartedAtUtc, s.EndedAtUtc ?? now),
                CoinDeltaOver(db, accountId, s.StartedAtUtc, s.EndedAtUtc ?? now)));
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, deltas.Values.SelectMany(d => d.Items.Keys).ToList());

        return sessions
            .Select(s =>
            {
                (Dictionary<int, int> items, long coin) = deltas[s.Id];
                long total = coin + items.Sum(kv => (long)kv.Value * values.GetValueOrDefault(kv.Key));
                return new GameSessionRow(
                    s.Id,
                    s.StartedAtUtc,
                    s.EndedAtUtc,
                    charactersBySession.TryGetValue(s.Id, out List<string>? chars) ? string.Join(", ", chars) : "—",
                    total,
                    mainMapBySession.GetValueOrDefault(s.Id));
            })
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

        // Reconstruct each segment's item + coin delta over its window, then value the items with one shared
        // price/vendor lookup (so a per-segment value can sit beside the existing gained/lost counts).
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var perSegment = segments
            .Select(seg => (
                Segment: seg,
                Items: ItemDeltaOver(db, accountId, seg.StartedAtUtc, seg.EndedAtUtc ?? now),
                Coin: CoinDeltaOver(db, accountId, seg.StartedAtUtc, seg.EndedAtUtc ?? now)))
            .ToList();
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, perSegment.SelectMany(x => x.Items.Keys).ToList());

        var rows = new List<SessionSegmentRow>();
        foreach ((CharacterSegment segment, Dictionary<int, int> items, long coin) in perSegment)
        {
            int gained = 0;
            int lost = 0;
            long loot = 0;
            foreach ((int itemId, int delta) in items)
            {
                if (delta > 0)
                {
                    gained += delta;
                }
                else
                {
                    lost += -delta;
                }

                loot += (long)delta * values.GetValueOrDefault(itemId);
            }

            rows.Add(new SessionSegmentRow(
                segment.Id, segment.Sequence, segment.CharacterName,
                segment.StartedAtUtc, segment.EndedAtUtc, coin, gained, lost, loot));
        }

        return rows;
    }

    /// <summary>The maps a whole session was played on, with the time on each, most-played first.</summary>
    public List<MapTimeRow> GetSessionMaps(string accountId, long gameSessionId) =>
        SafeRead(
            db =>
            {
                List<long> segmentIds = db.CharacterSegments.AsNoTracking()
                    .Where(s => s.GameSessionId == gameSessionId
                        && db.GameSessions.Any(g => g.Id == gameSessionId && g.AccountId == accountId))
                    .Select(s => s.Id)
                    .ToList();
                return TotalByMap(MapTimes(db, segmentIds));
            },
            new List<MapTimeRow>());

    /// <summary>The maps one character segment was played on, with the time on each, most-played first.</summary>
    public List<MapTimeRow> GetSegmentMaps(string accountId, long segmentId) =>
        SafeRead(
            db =>
            {
                bool owned = db.CharacterSegments.AsNoTracking().Any(s => s.Id == segmentId
                    && db.GameSessions.Any(g => g.Id == s.GameSessionId && g.AccountId == accountId));
                return owned ? TotalByMap(MapTimes(db, [segmentId])) : new List<MapTimeRow>();
            },
            new List<MapTimeRow>());

    /// <summary>Time on each map per character segment (open stretches measured to now), with map names.</summary>
    private static List<(long SegmentId, int MapId, string Name, TimeSpan Time)> MapTimes(
        Gw2GizmosDbContext db, IReadOnlyCollection<long> segmentIds)
    {
        if (segmentIds.Count == 0)
        {
            return [];
        }

        var stretches = db.MapSegments.AsNoTracking()
            .Where(m => segmentIds.Contains(m.CharacterSegmentId))
            .Select(m => new { m.CharacterSegmentId, m.MapId, m.StartedAtUtc, m.EndedAtUtc })
            .ToList();
        List<int> mapIds = stretches.Select(m => m.MapId).Distinct().ToList();
        Dictionary<int, string> names = db.Maps.AsNoTracking()
            .Where(m => mapIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.Type })
            .ToDictionary(m => m.Id, m => MapLabel.For(m.Name, m.Type));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return stretches
            .Select(m => (
                m.CharacterSegmentId,
                m.MapId,
                // Until the worker's first maps sync, an id is all there is.
                names.GetValueOrDefault(m.MapId) ?? $"Map {m.MapId}",
                (m.EndedAtUtc ?? now) - m.StartedAtUtc))
            .ToList();
    }

    /// <summary>Total time per map (by id, so same-named maps stay separate), most-played first.</summary>
    private static List<MapTimeRow> TotalByMap(IEnumerable<(long SegmentId, int MapId, string Name, TimeSpan Time)> times) =>
        times
            .GroupBy(t => t.MapId)
            .Select(g => new MapTimeRow(g.First().Name, TimeSpan.FromTicks(g.Sum(t => Math.Max(0, t.Time.Ticks)))))
            .OrderByDescending(m => m.Time)
            .ToList();

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
        Dictionary<int, long> itemValues = AccountHoldingsReconstructor.ItemValues(db, itemDeltas.Select(x => x.Id).ToList());
        List<SessionLootItem> items = itemDeltas
            .Select(x => new SessionLootItem(
                x.Id,
                itemNames.GetValueOrDefault(x.Id) ?? $"Item {x.Id}",
                x.Delta,
                x.Delta * itemValues.GetValueOrDefault(x.Id)))
            .OrderByDescending(i => i.ValueCopper)
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

    /// <summary>Estimated value of a whole session: coin gained plus the instant-sell value of net item loot,
    /// over the sitting's window (or up to now while it's still open).</summary>
    public SessionValueSummary GetSessionValue(string accountId, long gameSessionId) =>
        SafeRead(db => GetSessionValue(db, accountId, gameSessionId), SessionValueSummary.Empty);

    private static SessionValueSummary GetSessionValue(Gw2GizmosDbContext db, string accountId, long gameSessionId)
    {
        GameSession? session = db.GameSessions.AsNoTracking()
            .FirstOrDefault(s => s.Id == gameSessionId && s.AccountId == accountId);
        if (session is null)
        {
            return SessionValueSummary.Empty;
        }

        DateTimeOffset end = session.EndedAtUtc ?? DateTimeOffset.UtcNow;
        Dictionary<int, int> deltas = ItemDeltaOver(db, accountId, session.StartedAtUtc, end);
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, deltas.Keys.ToList());
        long loot = deltas.Sum(kv => (long)kv.Value * values.GetValueOrDefault(kv.Key));
        long coin = CoinDeltaOver(db, accountId, session.StartedAtUtc, end);
        return new SessionValueSummary(coin, loot, session.StartedAtUtc, session.EndedAtUtc);
    }

    /// <summary>Net per-item holdings delta over a window (end totals minus start totals); zero deltas dropped.</summary>
    private static Dictionary<int, int> ItemDeltaOver(Gw2GizmosDbContext db, string accountId, DateTimeOffset start, DateTimeOffset end)
    {
        Dictionary<int, int> startItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, start);
        Dictionary<int, int> endItems = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, end);

        var deltas = new Dictionary<int, int>();
        foreach (int id in startItems.Keys.Union(endItems.Keys))
        {
            int delta = endItems.GetValueOrDefault(id) - startItems.GetValueOrDefault(id);
            if (delta != 0)
            {
                deltas[id] = delta;
            }
        }

        return deltas;
    }

    private static long CoinDeltaOver(Gw2GizmosDbContext db, string accountId, DateTimeOffset start, DateTimeOffset end) =>
        AccountHoldingsReconstructor.WalletValueAsOf(db, accountId, CoinCurrencyId, end)
        - AccountHoldingsReconstructor.WalletValueAsOf(db, accountId, CoinCurrencyId, start);

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
                [DeletableData.TradingPost] = db.TradingPostOrders.Count(o => o.AccountId == accountId)
                    + db.TradingPostTransactions.Count(t => t.AccountId == accountId),
                [DeletableData.ValueHistory] = db.AccountValueSnapshots.Count(s => s.AccountId == accountId),
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
                    sessions.Count, total, character, isPlaying, latest.EndedAtUtc ?? latest.StartedAtUtc,
                    isPlaying ? latest.Id : null);
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

    /// <summary>The account's open trading-post orders, each with its item's competing prices from the last sync.</summary>
    public List<TradingPostOrderRow> GetTradingPostOrders(string accountId)
    {
        List<TradingPostOrder> orders = SafeRead(
            db => db.TradingPostOrders.AsNoTracking().Where(o => o.AccountId == accountId).ToList(),
            new List<TradingPostOrder>());
        Dictionary<int, string> names = LoadNames(orders.Select(o => o.ItemId), (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));

        return orders
            .Select(o => new TradingPostOrderRow(
                o.Id, o.Side == TradingPostSide.Buy, o.ItemId, names.GetValueOrDefault(o.ItemId, $"Item {o.ItemId}"),
                o.Price, o.Quantity, o.CreatedUtc, o.BestBuyPrice, o.BestSellPrice))
            // Beaten orders first (the ones needing attention), then newest.
            .OrderByDescending(o => o.IsBeaten)
            .ThenByDescending(o => o.CreatedUtc)
            .ToList();
    }

    /// <summary>The account's completed trades, newest first.</summary>
    public List<TradingPostTradeRow> GetTradingPostTrades(string accountId)
    {
        List<TradingPostTransaction> trades = SafeRead(
            db => db.TradingPostTransactions.AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.PurchasedUtc)
                .ToList(),
            new List<TradingPostTransaction>());
        Dictionary<int, string> names = LoadNames(trades.Select(t => t.ItemId), (d, ids) =>
            d.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new IdName(i.Id, i.Name)));

        return trades
            .Select(t => new TradingPostTradeRow(
                t.Id, t.Side == TradingPostSide.Sell, t.ItemId, names.GetValueOrDefault(t.ItemId, $"Item {t.ItemId}"),
                t.Price, t.Quantity, t.PurchasedUtc))
            .ToList();
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
    DateTimeOffset? LastPlayedUtc,
    long? OpenSessionId = null
);

/// <summary>One play session for the Sessions hub: when it ran, which characters were played, and the estimated
/// value (coin + instant-sell loot) earned that sitting.</summary>
public sealed record GameSessionRow(
    long Id,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    string CharactersDisplay,
    long TotalValueCopper,
    string? MainMap = null
)
{
    public bool IsActive => EndedAtUtc is null;

    public bool HasValue => TotalValueCopper != 0;

    /// <summary>Signed estimated value earned, e.g. "+42g 50s".</summary>
    public string ValueDisplay => SessionFormat.SignedCoin(TotalValueCopper);

    /// <summary>Hours the sitting lasted (open sessions measured to now); 0 when unknown/negative.</summary>
    private double DurationHours
    {
        get
        {
            TimeSpan span = (EndedAtUtc ?? DateTimeOffset.UtcNow) - StartedAtUtc;
            return span > TimeSpan.Zero ? span.TotalHours : 0;
        }
    }

    /// <summary>Estimated value earned per hour (copper), for sorting; 0 for sub-minute sittings.</summary>
    public long ProfitPerHourCopper => DurationHours <= 1.0 / 60 ? 0 : (long)(TotalValueCopper / DurationHours);

    /// <summary>Card title, e.g. "Mon 16 Jun, 19:04".</summary>
    public string Title => StartedAtUtc.LocalDateTime.ToString("ddd d MMM, HH:mm");

    /// <summary>E.g. "1h 45m · Dragon's End · Thena": the main map (most playtime) only, so a quick trip to a lounge
    /// doesn't clutter the list.</summary>
    public string Subtitle => MainMap is null
        ? $"{DurationDisplay} · {CharactersDisplay}"
        : $"{DurationDisplay} · {MainMap} · {CharactersDisplay}";

    public string DurationDisplay => SessionFormat.Duration(StartedAtUtc, EndedAtUtc);
}

/// <summary>A character as shown on its card: who it is, its profession (and elite specialization), and its crafting.</summary>
public sealed record CharacterCard(
    string Name,
    string Profession,
    string Race,
    string Gender,
    int Level,
    int AgeSeconds,
    DateTimeOffset Created,
    int Deaths,
    string? ProfessionIconUrl,
    string? EliteSpecialization,
    IReadOnlyList<CharacterCraftingLevel> Crafting
)
{
    /// <summary>E.g. "Level 80 Firebrand", or "Level 80 Guardian" without an elite specialization.</summary>
    public string Headline => $"Level {Level} {EliteSpecialization ?? Profession}";

    /// <summary>E.g. "Human · Female · Guardian" (the profession only when the headline shows the elite).</summary>
    public string Identity =>
        EliteSpecialization is null ? $"{Race} · {Gender}" : $"{Race} · {Gender} · {Profession}";

    /// <summary>E.g. "Level 80 Firebrand · Human · Female · Guardian".</summary>
    public string Summary => $"{Headline} · {Identity}";

    /// <summary>E.g. "1,234 h".</summary>
    public string PlayedText => $"{AgeSeconds / 3600:N0} h";

    public string DeathsText => $"{Deaths:N0}";

    /// <summary>E.g. "2014-05-12" (local date).</summary>
    public string CreatedDate => $"{Created.LocalDateTime:yyyy-MM-dd}";

    /// <summary>E.g. "1,234 h played · 321 deaths".</summary>
    public string PlayStats => $"{PlayedText} played · {DeathsText} {(Deaths == 1 ? "death" : "deaths")}";

    /// <summary>E.g. "Created 2014-05-12".</summary>
    public string CreatedText => $"Created {CreatedDate}";

    public bool HasCrafting => Crafting.Count > 0;
}

/// <summary>A crafting discipline a character has learned, e.g. Chef 400; the inactive ones are shown dimmed.</summary>
public sealed record CharacterCraftingLevel(string Discipline, int Rating, bool Active)
{
    public string Label => $"{Discipline} {Rating}";

    /// <summary>Inactive disciplines (learned, but not one of the two active) are dimmed.</summary>
    public double Opacity => Active ? 1.0 : 0.5;
}

/// <summary>
/// One open trading-post order. A sell listing is <b>undercut</b> when someone lists cheaper; a buy order is
/// <b>outbid</b> when someone offers more — <see cref="GapCopper"/> is by how much (as of the last sync).
/// </summary>
public sealed record TradingPostOrderRow(
    long Id,
    bool IsBuy,
    int ItemId,
    string Name,
    int Price,
    int Quantity,
    DateTimeOffset CreatedUtc,
    int? BestBuyPrice,
    int? BestSellPrice
)
{
    /// <summary>How far the order is behind the best competing price, in copper; null when it's the best.</summary>
    public long? GapCopper =>
        IsBuy
            ? BestBuyPrice > Price ? BestBuyPrice - Price : null
            : BestSellPrice < Price ? Price - BestSellPrice : null;

    public bool IsBeaten => GapCopper is not null;

    /// <summary>E.g. "Undercut by", "Outbid by", "Lowest listing", "Highest buy order".</summary>
    public string Standing => (IsBuy, IsBeaten) switch
    {
        (true, true) => "Outbid by",
        (false, true) => "Undercut by",
        (true, false) => "Highest buy order",
        (false, false) => "Lowest listing",
    };

    /// <summary>E.g. "Selling 25 · placed 2026-10-16 14:05" (local time).</summary>
    public string Details => $"{(IsBuy ? "Buying" : "Selling")} {Quantity:N0} · placed {CreatedUtc.LocalDateTime:yyyy-MM-dd HH:mm}";
}

/// <summary>A completed trade: something the account bought or sold on the trading post.</summary>
public sealed record TradingPostTradeRow(
    long Id,
    bool IsSell,
    int ItemId,
    string Name,
    int Price,
    int Quantity,
    DateTimeOffset PurchasedUtc
)
{
    // The trading post keeps a 10% exchange fee on a sale (the 5% listing fee was paid when listing).
    private const double SaleNetFactor = 0.90;

    public long TotalCopper => (long)Price * Quantity;

    /// <summary>What the trade did to the wallet: a sale's payout after the exchange fee (+), a purchase's cost (−).</summary>
    public long WalletCopper => IsSell ? (long)Math.Floor(TotalCopper * SaleNetFactor) : -TotalCopper;

    /// <summary>E.g. "Sold 25 × 1g 20s · 2026-10-16 14:05" (local time).</summary>
    public string Details =>
        $"{(IsSell ? "Sold" : "Bought")} {Quantity:N0} × {Coin.Format(Price)} · {PurchasedUtc.LocalDateTime:yyyy-MM-dd HH:mm}";
}

/// <summary>A map played during a session or segment, and the total time spent on it.</summary>
public sealed record MapTimeRow(string Name, TimeSpan Time)
{
    /// <summary>E.g. "1h 5m".</summary>
    public string TimeDisplay => SessionFormat.Span(Time);
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
    int ItemsLost,
    long LootValueCopper
)
{
    public bool IsActive => EndedAtUtc is null;

    /// <summary>Coin gained plus the instant-sell value of the net item loot.</summary>
    public long TotalValueCopper => CoinDelta + LootValueCopper;

    public bool HasValue => TotalValueCopper != 0;

    /// <summary>Signed estimated value earned in this segment, e.g. "+8g 12s".</summary>
    public string ValueDisplay => SessionFormat.SignedCoin(TotalValueCopper);

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

/// <summary>An item gained/lost over a segment, with the instant-sell gold value of that delta.</summary>
public sealed record SessionLootItem(int ItemId, string Name, int Delta, long ValueCopper)
{
    public string DeltaDisplay => (Delta >= 0 ? "+" : "−") + Math.Abs(Delta).ToString("N0");

    public bool HasValue => ValueCopper != 0;

    /// <summary>Signed gold value of the delta (instant-sell), e.g. "+12g 50s".</summary>
    public string ValueDisplay => SessionFormat.SignedCoin(ValueCopper);
}

/// <summary>A segment's full hoarded delta: currencies and items gained/lost over its window.</summary>
public sealed record SessionLoot(IReadOnlyList<SessionLootCurrency> Currencies, IReadOnlyList<SessionLootItem> Items)
{
    public bool HasCurrencies => Currencies.Count > 0;
    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => Currencies.Count == 0 && Items.Count == 0;
}

/// <summary>A session's estimated worth: coin gained plus the instant-sell value of net item loot, with the
/// gold-per-hour rate over the sitting's duration. Item value is the latest trading-post buy order minus the 15%
/// fee, falling back to vendor value for untradeable loot.</summary>
public sealed record SessionValueSummary(
    long CoinDeltaCopper,
    long LootValueCopper,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc
)
{
    public static SessionValueSummary Empty { get; } = new(0, 0, DateTimeOffset.UnixEpoch, null);

    public long TotalValueCopper => CoinDeltaCopper + LootValueCopper;

    public string TotalDisplay => SessionFormat.SignedCoin(TotalValueCopper);

    public string CoinDisplay => SessionFormat.SignedCoin(CoinDeltaCopper);

    public string LootDisplay => SessionFormat.SignedCoin(LootValueCopper);

    /// <summary>Estimated value earned per hour over the sitting, in copper; null for sub-minute sessions.</summary>
    public long? GoldPerHourCopper
    {
        get
        {
            double hours = ((EndedAtUtc ?? DateTimeOffset.UtcNow) - StartedAtUtc).TotalHours;
            return hours <= 1.0 / 60 ? null : (long)(TotalValueCopper / hours);
        }
    }

    /// <summary>Estimated value earned per hour over the sitting, e.g. "+12g 30s / h" (— for sub-minute sessions).</summary>
    public string GoldPerHourDisplay
    {
        get
        {
            if (GoldPerHourCopper is not { } perHour)
            {
                return "—";
            }

            return SessionFormat.SignedCoin(perHour) + " / h";
        }
    }
}

/// <summary>Shared formatting for the session views (durations and signed coin amounts).</summary>
internal static class SessionFormat
{
    public static string Duration(DateTimeOffset start, DateTimeOffset? end) =>
        Span((end ?? DateTimeOffset.UtcNow) - start);

    /// <summary>A length of play time, e.g. "1h 5m", "25m", "40s".</summary>
    public static string Span(TimeSpan span)
    {
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
