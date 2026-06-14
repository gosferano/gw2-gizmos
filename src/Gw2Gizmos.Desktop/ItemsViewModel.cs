using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Items grid: lists every tradeable or craftable item, overlaying the worker's precomputed
/// <see cref="MarketItem"/> snapshot (best buy/sell prices, demand/supply, and craft cost/profit) on the
/// tradeable ones. A craftable-but-non-tradeable item still appears — with blank market columns — and its
/// craft tree is rebuilt on demand like any other. Items that are neither are omitted (nothing to show).
/// Exposes a name-filterable view; sort by the Profit or Margin column for the "what's profitable to craft" view.
/// The tree is never stored — it's recomputed for the one row in focus, which is cheap because the engine memoizes.
/// </summary>
public sealed class ItemsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private MarketItem? _selectedItem;
    private IReadOnlyList<RecipeNode> _selectedTree = Array.Empty<RecipeNode>();
    private string _filterText = "";

    public ItemsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        // The worker owns the database (opened read-only here); on a fresh install it may not exist yet.
        // Treat an absent/locked DB as an empty list rather than crashing the page.
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            // The market snapshot covers only tradeable items: best buy/sell plus computed craft cost/profit.
            Dictionary<int, MarketItem> market = dbContext.MarketItems.AsNoTracking().ToDictionary(m => m.ItemId);

            // Every item any recipe outputs — craftable even if it never reaches the trading post.
            HashSet<int> craftableIds = dbContext.Recipes.AsNoTracking()
                .Select(r => r.OutputItemId).Distinct().ToHashSet();

            // List every item that's tradeable or craftable (or both). A tradeable item carries its market
            // snapshot; a craftable-but-non-tradeable item still gets a row so its craft tree is reachable.
            // Items that are neither are skipped — both detail panes would be empty, so they'd only be noise.
            foreach (var item in dbContext.Items.AsNoTracking()
                         .Select(i => new { i.Id, i.Name })
                         .OrderBy(i => i.Name))
            {
                if (market.TryGetValue(item.Id, out MarketItem? snapshot))
                {
                    Items.Add(snapshot);
                }
                else if (craftableIds.Contains(item.Id))
                {
                    Items.Add(new MarketItem
                    {
                        ItemId = item.Id,
                        Name = item.Name,
                        IsCraftable = true,
                    });
                }
            }

            // All snapshot rows share one batch timestamp, so any of them dates the market data.
            ComputedAt = market.Count > 0 ? market.Values.First().ComputedAtUtc.LocalDateTime : null;
        }
        catch (Exception)
        {
            // Nothing synced yet — the grid renders empty until the worker produces items.
        }

        View = CollectionViewSource.GetDefaultView(Items);
        View.Filter = MatchesFilter;
    }

    public ObservableCollection<MarketItem> Items { get; } = new();

    /// <summary>The grid binds here so sorting and the text filter apply without disturbing <see cref="Items"/>.</summary>
    public ICollectionView View { get; }

    /// <summary>When the worker last produced this snapshot; null when it has never run.</summary>
    public DateTimeOffset? ComputedAt { get; }

    /// <summary>Free-text name filter applied to the grid.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                View.Refresh();
            }
        }
    }

    public MarketItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                _ = LoadDetailsAsync(value);
            }
        }
    }

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
        || (obj is MarketItem item && item.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Loads the selected row's detail panes (craft tree + price history) as fire-and-forget work, but
    /// observes any failure here so a detail-load error degrades the pane gracefully instead of surfacing
    /// as an unobserved task exception.
    /// </summary>
    private async Task LoadDetailsAsync(MarketItem? item)
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

    private async Task UpdateTreeAsync(MarketItem? item)
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

    private async Task UpdateHistoryAsync(MarketItem? item)
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
