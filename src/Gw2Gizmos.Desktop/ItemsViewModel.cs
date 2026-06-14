using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Items grid: lists every tradeable or craftable item. Buy/sell/demand/supply come live from the
/// most recent price poll (<c>PriceSnapshots</c>) — the same source the history chart uses, so the grid and
/// the chart can never disagree. Craft cost comes from the worker's <c>ItemCraftCosts</c> cache (the one
/// expensive precompute), and profit/margin are derived here from the live buy price. A craftable-but-non-
/// tradeable item still appears (blank price columns); items that are neither are omitted (nothing to show).
/// Exposes a name-filterable view; sort by Profit or Margin for the "what's profitable to craft" view. The
/// craft tree is rebuilt on demand for the row in focus — cheap because the engine memoizes — never stored.
/// </summary>
public sealed class ItemsViewModel : ViewModelBase
{
    /// <summary>Trading-post sales are charged a 15% fee (listing + exchange), so you net 85%.</summary>
    private const double TradingPostNetFactor = 0.85;

    private readonly IServiceScopeFactory _scopeFactory;
    private ItemRow? _selectedItem;
    private IReadOnlyList<RecipeNode> _selectedTree = Array.Empty<RecipeNode>();
    private string _filterText = "";
    private bool _isRefreshing;
    private ObservableCollection<ItemRow> _items = new();
    private ICollectionView _view;
    private readonly DispatcherTimer _filterDebounce;

    public ItemsViewModel(IServiceScopeFactory scopeFactory, FeatureSettingsStore features)
    {
        _scopeFactory = scopeFactory;
        // Read once — the VM is transient, so this reflects the toggle's state at navigation time.
        PricesEnabled = features.IsEnabled(WorkerFeatures.PricesSync.Key);
        _view = CollectionViewSource.GetDefaultView(_items);
        _view.Filter = MatchesFilter;
        // Re-filtering the (large) grid on every keystroke blocks the UI thread, so each typed character lags.
        // Debounce instead: keystrokes restart the timer and the filter applies once typing pauses.
        _filterDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _filterDebounce.Tick += (_, _) =>
        {
            _filterDebounce.Stop();
            View.Refresh();
        };
        // Re-reads the grid from the worker-owned DB (picking up newer prices). The text filter is a view filter
        // keyed off _filterText, untouched by a reload, so the current search stays applied to the fresh rows.
        RefreshCommand = new RelayCommand(() => _ = LoadAsync(), () => !IsRefreshing);
        // Load off the UI thread (the first DB touch also pays EF's cold-start cost). The await resumes on
        // the UI thread, where swapping in the bound collection/properties is safe.
        _ = LoadAsync();
    }

    /// <summary>Loads the grid asynchronously so navigating to the page never blocks the UI thread. Also the
    /// Refresh handler — guarded so an in-flight load isn't stacked by an impatient second click.</summary>
    private async Task LoadAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            (List<ItemRow> rows, DateTimeOffset? updatedAt) = await Task.Run(LoadRows);

            // Build the collection in one shot (the ObservableCollection constructor copies without per-item
            // events) and swap in a fresh view, rather than raising thousands of CollectionChanged events.
            _items = new ObservableCollection<ItemRow>(rows);
            ICollectionView view = CollectionViewSource.GetDefaultView(_items);
            view.Filter = MatchesFilter;
            View = view;

            PricesUpdatedAt = updatedAt;
            OnPropertyChanged(nameof(PricesUpdatedAt));
            OnPropertyChanged(nameof(PricesSyncing));
            OnPropertyChanged(nameof(PricesStatus));
        }
        catch (Exception)
        {
            // Nothing synced yet / read failed — the grid stays empty.
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// The heavy read, run on a background thread. The worker owns the database (opened read-only here); on a
    /// fresh install it may not exist yet, in which case the query throws and <see cref="LoadAsync"/> leaves
    /// the grid empty.
    /// </summary>
    private (List<ItemRow> Rows, DateTimeOffset? UpdatedAt) LoadRows()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        // Live market figures: the most recent price poll per item (best buy/sell + demand/supply). Skipped
        // entirely when price history is off, so stale snapshots from an earlier run don't read as current.
        Dictionary<int, PricePoint> prices = new();
        DateTimeOffset? updatedAt = null;
        if (PricesEnabled)
        {
            IQueryable<long> latestIds = db.PriceSnapshots
                .GroupBy(snapshot => snapshot.ItemId)
                .Select(group => group.Max(snapshot => snapshot.Id));
            var latest = db.PriceSnapshots.AsNoTracking()
                .Where(snapshot => latestIds.Contains(snapshot.Id))
                .Select(snapshot => new
                {
                    snapshot.ItemId,
                    snapshot.Buy,
                    snapshot.Sell,
                    snapshot.Demand,
                    snapshot.Supply,
                    snapshot.TimestampUtc
                })
                .ToList();
            prices = latest.ToDictionary(
                row => row.ItemId,
                row => new PricePoint(row.Buy, row.Sell, row.Demand, row.Supply)
            );
            // Show the latest poll's time so users can see how fresh the grid's prices are.
            updatedAt = latest.Count > 0 ? latest.Max(row => row.TimestampUtc).LocalDateTime : null;
        }

        // Cached craft cost per craftable item (the one expensive precompute). Ignored when price history is off
        // — without ingredient prices the cache fills with zeros, which would read as a misleading "0c".
        Dictionary<int, double> craftCosts = PricesEnabled
            ? db.ItemCraftCosts.AsNoTracking().ToDictionary(cost => cost.ItemId, cost => cost.CraftingCost)
            : new Dictionary<int, double>();

        // Every item any recipe outputs — craftable even if it never reaches the trading post.
        HashSet<int> craftableIds = db.Recipes.AsNoTracking()
            .Select(recipe => recipe.OutputItemId).Distinct().ToHashSet();

        // Every item with a trading-post listing — "tradeable" independent of whether prices are being recorded,
        // so tradeable items still appear (with blank price columns) when price history is off.
        HashSet<int> tradeableIds = db.PriceSnapshots.AsNoTracking()
            .Select(snapshot => snapshot.ItemId).ToHashSet();

        // List every item that's tradeable or craftable (or both). Items that are neither are skipped — both
        // detail panes would be empty, so they'd only be noise.
        var rows = new List<ItemRow>();
        foreach (var item in db.Items.AsNoTracking()
                     .Select(i => new { i.Id, i.Name })
                     .OrderBy(i => i.Name))
        {
            bool tradeable = tradeableIds.Contains(item.Id);
            bool craftable = craftableIds.Contains(item.Id);
            if (!tradeable && !craftable)
            {
                continue;
            }

            bool hasPrice = prices.TryGetValue(item.Id, out PricePoint price);
            double? craftCost = craftCosts.TryGetValue(item.Id, out double cost) ? cost : null;
            // Profit (and margin) two ways: dumping into the buy orders for an instant sale, and listing at
            // the sell price — each nets the 15% trading-post fee, minus craft cost. Only meaningful when the
            // item is craftable and that side is priced, otherwise blank.
            double? buyProfit = craftCost is double buyCost && price.Buy is int buy
                ? (buy * TradingPostNetFactor) - buyCost
                : null;
            double? sellProfit = craftCost is double sellCost && price.Sell is int sell
                ? (sell * TradingPostNetFactor) - sellCost
                : null;
            double? buyMargin = buyProfit is double bp && craftCost is double bc and > 0 ? bp / bc * 100d : null;
            double? sellMargin = sellProfit is double sp && craftCost is double sc and > 0 ? sp / sc * 100d : null;

            rows.Add(new ItemRow(
                item.Id,
                item.Name,
                hasPrice ? price.Buy : null,
                hasPrice ? price.Demand : null,
                hasPrice ? price.Sell : null,
                hasPrice ? price.Supply : null,
                craftCost,
                buyProfit,
                buyMargin,
                sellProfit,
                sellMargin,
                craftable
            ));
        }

        return (rows, updatedAt);
    }

    /// <summary>The grid binds here so sorting and the text filter apply over the latest loaded rows.</summary>
    public ICollectionView View
    {
        get => _view;
        private set => SetProperty(ref _view, value);
    }

    /// <summary>Reloads the grid from the database, keeping the current search filter applied.</summary>
    public RelayCommand RefreshCommand { get; }

    /// <summary>True while a (re)load is in flight; disables the Refresh button so loads can't stack.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                // Re-evaluate RefreshCommand.CanExecute now that the in-flight state changed.
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>When the latest price poll ran; null when no prices have been recorded yet.</summary>
    public DateTimeOffset? PricesUpdatedAt { get; private set; }

    /// <summary>Whether the Price history feature is on. When off, the price/craft columns and the craft tree's
    /// craft-vs-buy economics are simply absent (not "syncing"), and the header says so.</summary>
    public bool PricesEnabled { get; }

    /// <summary>
    /// True only during a genuine cold start: price history is enabled but the first <c>PriceSnapshot</c> hasn't
    /// landed yet, so every item still reads as 0 — the craft tree is blanked behind a "syncing" note until then.
    /// When price history is <em>off</em> this is false: the page shows its structure with prices absent rather
    /// than a perpetual syncing note.
    /// </summary>
    public bool PricesSyncing => PricesEnabled && PricesUpdatedAt is null;

    /// <summary>Header status: price history off, the first-poll syncing note, or when prices last polled.</summary>
    public string PricesStatus =>
        !PricesEnabled ? "Price history is off — enable it in Settings to see prices and craft costs."
        : PricesUpdatedAt is { } updated ? $"Prices as of {updated:yyyy-MM-dd HH:mm}."
        : "Prices syncing…";

    /// <summary>Free-text name filter applied to the grid.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                // Restart the debounce; the filter runs on the next pause, not on this keystroke.
                _filterDebounce.Stop();
                _filterDebounce.Start();
            }
        }
    }

    public ItemRow? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HistoryPlaceholder));
                _ = LoadDetailsAsync(value);
            }
        }
    }

    /// <summary>Text for the empty history pane: a prompt when nothing is selected, otherwise a note that the
    /// selected item has no recorded price history (e.g. an untradeable item never reaches the price poll).</summary>
    public string HistoryPlaceholder =>
        _selectedItem is null ? "Select an item to see its price history." : "No price history for this item.";

    /// <summary>The selected item's craft tree as a single-root list for the detail <c>TreeView</c>.</summary>
    public IReadOnlyList<RecipeNode> SelectedTree
    {
        get => _selectedTree;
        private set => SetProperty(ref _selectedTree, value);
    }

    /// <summary>The selected item's trading-post price/volume history charts.</summary>
    public PriceHistoryViewModel History { get; } = new();

    private bool MatchesFilter(object obj) =>
        string.IsNullOrWhiteSpace(_filterText)
        || (obj is ItemRow item && item.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Loads the selected row's detail panes (craft tree + price history) as fire-and-forget work, but
    /// observes any failure here so a detail-load error degrades the pane gracefully instead of surfacing
    /// as an unobserved task exception.
    /// </summary>
    private async Task LoadDetailsAsync(ItemRow? item)
    {
        try
        {
            await UpdateTreeAsync(item);
        }
        catch
        {
            if (_selectedItem?.ItemId == item?.ItemId)
            {
                SelectedTree = Array.Empty<RecipeNode>();
            }
        }

        try
        {
            await UpdateHistoryAsync(item);
        }
        catch
        {
            if (_selectedItem?.ItemId == item?.ItemId)
            {
                History.Load(Array.Empty<HistoryPoint>());
            }
        }
    }

    private async Task UpdateTreeAsync(ItemRow? item)
    {
        if (item is null || !item.IsCraftable)
        {
            SelectedTree = Array.Empty<RecipeNode>();
            return;
        }

        // Build off the UI thread; one item's tree is a handful of memoized queries even for deep chains.
        RecipeNode root = await Task.Run(async () =>
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            var builder = new RecipeTreeBuilder(dbContext, RecipeFinder.Configuration.Default);
            return await builder.BuildTreeAsync(item.ItemId, CancellationToken.None);
        });

        // A slow build may finish after the selection moved on; only apply if this row is still selected.
        if (_selectedItem?.ItemId == item.ItemId)
        {
            SelectedTree = new[] { root };
        }
    }

    private async Task UpdateHistoryAsync(ItemRow? item)
    {
        if (item is null)
        {
            History.Load(Array.Empty<HistoryPoint>());
            return;
        }

        HistoryPoint[] points = await Task.Run(() =>
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            // Order by Id, not TimestampUtc: SQLite can't ORDER BY a DateTimeOffset, and Id is monotonic
            // with insertion time (and stays so through downsampling, which keeps each bucket's latest Id).
            return dbContext
                .PriceSnapshots.AsNoTracking()
                .Where(snapshot => snapshot.ItemId == item.ItemId)
                .OrderBy(snapshot => snapshot.Id)
                .Select(snapshot => new HistoryPoint(
                    snapshot.TimestampUtc.LocalDateTime,
                    snapshot.Buy,
                    snapshot.Sell,
                    snapshot.Sold + snapshot.Bought
                ))
                .ToArray();
        });

        // Ignore a stale load if the selection moved on while querying.
        if (_selectedItem?.ItemId == item.ItemId)
        {
            History.Load(points);
        }
    }
}

/// <summary>One row of the Items grid: identity plus live market figures and craft economics. Price columns
/// are null when the item has never been polled; craft columns are null when it isn't craftable. Profit/margin
/// come two ways — selling into buy orders, and listing at the sell price.</summary>
public sealed record ItemRow(
    int ItemId,
    string Name,
    int? Buy,
    int? Demand,
    int? Sell,
    int? Supply,
    double? CraftingCost,
    double? BuyProfit,
    double? BuyMargin,
    double? SellProfit,
    double? SellMargin,
    bool IsCraftable
);

/// <summary>The latest poll's market figures for one item; Buy/Sell are null when that side has no orders.</summary>
internal readonly record struct PricePoint(int? Buy, int? Sell, int Demand, int Supply);
