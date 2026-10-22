using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Core.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Finds pages, characters, events and items by name for the global search. Pages and events are static lists;
/// characters and the items the Items page lists (tradeable or craftable) are read once per opening
/// (<see cref="PrepareAsync"/>), and items are matched in memory, typo-tolerant (<see cref="ItemMatcher"/>). Within
/// each kind, names that start with the query rank first, then names with a word starting with it, then names merely
/// containing it.
/// </summary>
public sealed class GlobalSearch
{
    private const int PageLimit = 5;
    private const int CharacterLimit = 5;
    private const int EventLimit = 5;
    private const int ItemLimit = 8;

    /// <summary>Every page worth jumping to, with extra words it should be found by.</summary>
    private static readonly SearchPage[] Pages =
    {
        new("Dashboard", null, "Home24", AppPage.Dashboard, "home overview"),
        new("Notifications", null, "Alert24", AppPage.Notifications, "alerts toasts"),
        new("Events", null, "Clock24", AppPage.Events, "timers world bosses metas"),
        new("Daily and weekly", null, "CalendarCheckmark24", AppPage.ResetProgress,
            "dailies weeklies reset wizard's vault astral acclaim world bosses map chests daily crafting raids"),
        new("World map", null, "Map24", AppPage.WorldMap, "tyria waypoints location"),
        new("WvW", null, "Shield24", AppPage.Wvw, "world vs world matchup borderlands skirmish team"),
        new("Achievements", "Account", "Trophy24", AppPage.Achievements, "ap points collections progress quick wins"),
        new("Items", null, "BoxMultiple24", AppPage.Items, "market prices crafting"),
        new("Trading post", null, "BuildingShop24", AppPage.TradingPost, "tp"),
        new("Orders", "Trading post", "BuildingShop24", AppPage.TradingPostOrders,
            "tp buy sell listings undercut outbid stale"),
        new("History", "Trading post", "History24", AppPage.TradingPostHistory, "tp trades bought sold transactions"),
        new("Profit", "Trading post", "DataTrending24", AppPage.TradingPostProfit,
            "tp ledger realized profit margin fees income flipping"),
        new("Price alerts", "Trading post", "AlertOn24", AppPage.PriceAlerts, "tp notify price watch"),
        new("Advisor", null, "Lightbulb24", AppPage.Advisor, "advice suggestions"),
        new("Craft from stock", "Advisor", "Wrench24", AppPage.CraftFromStock,
            "crafting profit materials owned storage what to craft"),
        new("Inventory cleanup", "Advisor", "Broom24", AppPage.InventoryCleanup,
            "bank bags stack deposit sell open containers declutter space slots"),
        new("Account", null, "Person24", AppPage.Account, ""),
        new("Value", "Account", "DataTrending24", AppPage.AccountValue, "account value worth net gold"),
        new("Wallet", "Account", "Wallet24", AppPage.Wallet, "currencies coin karma"),
        new("Material storage", "Account", "BoxMultiple24", AppPage.MaterialStorage, "materials"),
        new("Bank", "Account", "BuildingBank24", AppPage.Bank, ""),
        new("Shared inventory", "Account", "Backpack24", AppPage.SharedInventory, "slots"),
        new("Characters", "Account", "People24", AppPage.Characters, ""),
        new("Sessions", null, "History24", AppPage.Sessions, "play sessions loot profit"),
        new("Logs", null, "DocumentBulletList24", AppPage.Logs, ""),
        new("API keys", null, "Key24", AppPage.ApiKeys, "token permissions"),
        new("Settings", null, "Settings24", AppPage.Settings, "features options"),
        new("Advanced settings", "Settings", "Options24", AppPage.AdvancedSettings, "sync intervals"),
        new("Stored data", "Settings", "Delete24", AppPage.StoredData, "delete history"),
    };

    /// <summary>
    /// Pages that can't be jumped to on their own — they show one particular character, session or segment, or are the
    /// first-run welcome. Every other page must be in the search (a test checks every page is in one list or the other).
    /// </summary>
    public static IReadOnlySet<AppPage> NotSearchable { get; } = new HashSet<AppPage>
    {
        AppPage.Onboarding,
        AppPage.Character,
        AppPage.CharacterInventory,
        AppPage.Session,
        AppPage.SessionLoot,
    };

    /// <summary>The pages the search finds.</summary>
    public static IReadOnlyCollection<AppPage> SearchablePages { get; } = Pages.Select(p => p.Target).ToArray();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AccountReader _reader;
    private readonly SelectedAccountService _account;
    private readonly ReminderSettingsStore _reminderSettings;
    private IReadOnlyList<CharacterCard> _characters = Array.Empty<CharacterCard>();
    private IReadOnlyList<SearchableItem> _items = Array.Empty<SearchableItem>();

    public GlobalSearch(
        IServiceScopeFactory scopeFactory,
        AccountReader reader,
        SelectedAccountService account,
        ReminderSettingsStore reminderSettings)
    {
        _scopeFactory = scopeFactory;
        _reader = reader;
        _account = account;
        _reminderSettings = reminderSettings;
    }

    /// <summary>Reads what's searched in memory: the account's characters, and which items the Items page lists.</summary>
    public async Task PrepareAsync()
    {
        string? accountId = _account.AccountId;
        (List<CharacterCard> characters, List<SearchableItem> items) = await Task.Run(() =>
            (accountId is null ? new List<CharacterCard>() : _reader.GetCharacterCards(accountId), ReadListedItems()));
        _characters = characters;
        _items = items;
    }

    /// <summary>Results for <paramref name="query"/>, grouped by kind (pages, characters, events, items).</summary>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        query = query.Trim();
        if (query.Length == 0)
        {
            // Nothing typed yet: every page, for quick keyboard navigation.
            return Pages.Select(ToResult).ToList();
        }

        var results = new List<SearchResult>();
        results.AddRange(Best(Pages, p => new[] { p.Title, p.Keywords }, query, PageLimit).Select(ToResult));
        results.AddRange(Best(_characters, c => new[] { c.Name, c.Profession, c.EliteSpecialization }, query, CharacterLimit)
            .Select(c => new SearchResult(SearchResultKind.Character, c.Name, c.Summary)
            {
                IconUrl = c.ProfessionIconUrl,
                CharacterName = c.Name,
            }));
        results.AddRange(Best(SearchableEvents(), e => new[] { e.Name, e.Map }, query, EventLimit)
            .Select(e => new SearchResult(SearchResultKind.Event, e.Name, e.Map) { Symbol = "Clock24", EventName = e.Name }));

        if (query.Length >= 2)
        {
            IReadOnlyList<SearchableItem> searchable = _items;
            List<SearchableItem> items = await Task.Run(() => ItemMatcher.Find(searchable, query, ItemLimit), cancellationToken);
            // The rarity tells same-named items apart (e.g. an exotic and an ascended Fiber Splicer).
            results.AddRange(items.Select(item => new SearchResult(
                SearchResultKind.Item, item.Name, $"{item.Rarity} · {(item.Tradeable ? "Tradeable" : "Craftable")}")
            {
                ItemId = item.Id,
            }));
        }

        return results;
    }

    /// <summary>How well <paramref name="text"/> matches <paramref name="query"/>: 3 starts with it, 2 has a word
    /// starting with it, 1 contains it, 0 no match (case-insensitive).</summary>
    public static int Score(string? text, string query)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return 0;
        }

        if (index == 0)
        {
            return 3;
        }

        // Any later occurrence at the start of a word ("coin" in "Mystic Coin").
        for (int i = index; i >= 0; i = text.IndexOf(query, i + 1, StringComparison.OrdinalIgnoreCase))
        {
            if (!char.IsLetterOrDigit(text[i - 1]))
            {
                return 2;
            }
        }

        return 1;
    }

    private static IEnumerable<T> Best<T>(IEnumerable<T> candidates, Func<T, IEnumerable<string?>> names, string query, int limit) =>
        candidates
            .Select(c => (Candidate: c, Score: names(c).Max(n => Score(n, query))))
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .Take(limit)
            .Select(c => c.Candidate);

    private IEnumerable<ScheduledEvent> SearchableEvents()
    {
        // Festival events only show on the Events page while their festival is the one marked as running.
        Festival? festival = _reminderSettings.SelectedFestival;
        return EventSchedule.All
            .Where(e => e.Festival is null || e.Festival == festival)
            .DistinctBy(e => e.Name);
    }

    /// <summary>The items the Items page lists — tradeable (seen on the trading post) or craftable — with their names,
    /// for matching in memory.</summary>
    private List<SearchableItem> ReadListedItems()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            Dictionary<int, bool> listed = ItemLinks.ReadListed(db);
            return db.Items.AsNoTracking()
                .Select(i => new { i.Id, i.Name, i.Rarity })
                .AsEnumerable()
                .Where(i => listed.ContainsKey(i.Id))
                .Select(i => new SearchableItem(i.Id, i.Name, i.Rarity, listed[i.Id]))
                .ToList();
        }
        catch (Exception)
        {
            // Database not ready yet: no item results.
            return new List<SearchableItem>();
        }
    }

    private static SearchResult ToResult(SearchPage page) =>
        new(SearchResultKind.Page, page.Title, page.Section ?? "") { Symbol = page.Symbol, Page = page.Target };

    private sealed record SearchPage(string Title, string? Section, string Symbol, AppPage Target, string Keywords);
}

public enum SearchResultKind
{
    Page,
    Character,
    Event,
    Item,
}

/// <summary>
/// One global search result: what it is, how it's shown (a Fluent icon, an icon URL or an item icon), and the target
/// it opens.
/// </summary>
public sealed record SearchResult(SearchResultKind Kind, string Title, string Subtitle)
{
    /// <summary>A Fluent icon name (the desktop's FluentSymbol), for pages and events.</summary>
    public string? Symbol { get; init; }

    /// <summary>An icon by URL, e.g. a character's profession icon.</summary>
    public string? IconUrl { get; init; }

    public int? ItemId { get; init; }

    public AppPage? Page { get; init; }

    public string? CharacterName { get; init; }

    public string? EventName { get; init; }

    public bool HasSymbol => Symbol is not null;

    public bool HasIconUrl => IconUrl is not null;

    public bool HasItem => ItemId is not null;

    public bool HasSubtitle => Subtitle.Length > 0;

    /// <summary>E.g. "Page", "Character" — shown at the row's end.</summary>
    public string KindLabel => Kind.ToString();
}
