using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A completed trade on the History page, and for a sale, what it made.</summary>
public sealed record TradingPostHistoryRow(TradingPostTradeRow Trade, TradingPostSale? Sale)
{
    /// <summary>
    /// A sale's profit on the units matched to earlier purchases; null for a purchase or an unmatched sale.
    /// </summary>
    public long? ProfitCopper => Sale?.Profit;

    public bool HasProfit => ProfitCopper is not null;

    /// <summary>For a sale none of whose units matched a purchase: its proceeds are income, not profit.</summary>
    public bool IsIncomeOnly => Sale is { Profit: null };

    /// <summary>For a sale only partly matched, e.g. "3 of 5 not bought here".</summary>
    public string? UnmatchedNote =>
        Sale is { MatchedQuantity: > 0 } sale && sale.MatchedQuantity < sale.Quantity
            ? $"{sale.Quantity - sale.MatchedQuantity:N0} of {sale.Quantity:N0} not bought here"
            : null;

    public bool HasUnmatchedNote => UnmatchedNote is not null;
}

/// <summary>
/// Backs the Trading post › History page: the account's completed trades, newest first, filterable to buys or sells,
/// a page at a time; each sale shows its profit. Refreshes after syncs.
/// </summary>
public sealed class TradingPostHistoryViewModel : ViewModelBase, INavigationAware
{
    private const int PageSize = 200;

    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private List<TradingPostHistoryRow> _all = [];
    private int _selectedFilterIndex;
    private int _shown = PageSize;
    private bool _loaded;

    public TradingPostHistoryViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges
    )
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = TradingPostBreadcrumbs.Section("History");
        ShowMoreCommand = new RelayCommand(() =>
        {
            _shown += PageSize;
            Apply();
        });
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    /// <summary>The filtered trades currently shown (the first pages of them).</summary>
    public ObservableCollection<TradingPostHistoryRow> Trades { get; } = new();

    public IReadOnlyList<string> FilterOptions { get; } = ["All", "Buys", "Sells"];

    public int SelectedFilterIndex
    {
        get => _selectedFilterIndex;
        set
        {
            if (SetProperty(ref _selectedFilterIndex, value))
            {
                _shown = PageSize;
                Apply();
            }
        }
    }

    public bool NoTrades => _loaded && Trades.Count == 0;

    public bool HasMore { get; private set; }

    /// <summary>E.g. "Showing 200 of 1,203 trades".</summary>
    public string Summary { get; private set; } = "";

    public RelayCommand ShowMoreCommand { get; }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            _all = await Task.Run(() =>
            {
                List<TradingPostTradeRow> trades = _reader.GetTradingPostTrades(accountId);
                Dictionary<long, TradingPostSale> sales = TradingPostLedger.Sales(trades).ToDictionary(s => s.TradeId);
                return trades.Select(t => new TradingPostHistoryRow(t, sales.GetValueOrDefault(t.Id))).ToList();
            });
            _loaded = true;
            Apply();
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void Apply()
    {
        List<TradingPostHistoryRow> filtered = (_selectedFilterIndex switch
        {
            1 => _all.Where(r => !r.Trade.IsSell),
            2 => _all.Where(r => r.Trade.IsSell),
            _ => _all,
        }).ToList();
        Trades.ReplaceWith(filtered.Take(_shown));
        HasMore = filtered.Count > Trades.Count;
        Summary = filtered.Count == 0 ? "" : $"Showing {Trades.Count:N0} of {filtered.Count:N0} trades";
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(NoTrades));
    }
}
