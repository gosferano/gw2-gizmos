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
/// Backs the Market grid: loads the worker's precomputed <see cref="MarketItem"/> snapshot (every
/// tradeable item with its best buy/sell prices, demand/supply, and craft cost/profit where craftable),
/// exposes a name-filterable view, and rebuilds the selected item's craft tree on demand. The tree is
/// never stored — it's recomputed for the one row in focus, which is cheap because the engine memoizes.
/// Sort by the Profit or Margin column to get the "what's profitable to craft" view.
/// </summary>
public sealed class MarketViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private MarketItem? _selectedItem;
    private IReadOnlyList<RecipeNode> _selectedTree = Array.Empty<RecipeNode>();
    private string _filterText = "";

    public MarketViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        using IServiceScope scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        foreach (MarketItem item in dbContext.MarketItems.AsNoTracking().OrderBy(item => item.Name))
        {
            Items.Add(item);
        }

        View = CollectionViewSource.GetDefaultView(Items);
        View.Filter = MatchesFilter;

        ComputedAtUtc = Items.Count > 0 ? Items[0].ComputedAtUtc : null;
    }

    public ObservableCollection<MarketItem> Items { get; } = new();

    /// <summary>The grid binds here so sorting and the text filter apply without disturbing <see cref="Items"/>.</summary>
    public ICollectionView View { get; }

    /// <summary>When the worker last produced this snapshot; null when it has never run.</summary>
    public DateTimeOffset? ComputedAtUtc { get; }

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
                _ = UpdateTreeAsync(value);
            }
        }
    }

    /// <summary>The selected item's craft tree as a single-root list for the detail <c>TreeView</c>.</summary>
    public IReadOnlyList<RecipeNode> SelectedTree
    {
        get => _selectedTree;
        private set => SetProperty(ref _selectedTree, value);
    }

    private bool MatchesFilter(object obj) =>
        string.IsNullOrWhiteSpace(_filterText)
        || (obj is MarketItem item && item.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

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
}
