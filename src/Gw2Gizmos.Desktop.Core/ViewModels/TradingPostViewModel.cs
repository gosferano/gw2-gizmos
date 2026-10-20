using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Trading post page for the selected account: its open orders (undercut / outbid ones first) and a log
/// of its completed trades, newest first, filterable to buys or sells. The log is long (the API keeps ~90 days), so
/// it's shown a page at a time. Refreshes in place after each account sync.
/// </summary>
public sealed class TradingPostViewModel : ViewModelBase, INavigationAware
{
    private const int TradesPageSize = 200;

    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private List<TradingPostTradeRow> _allTrades = new();
    private int _selectedTradeFilterIndex;
    private int _tradesShown = TradesPageSize;
    private bool _loaded;

    public TradingPostViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges,
        PriceAlertsViewModel priceAlerts
    )
    {
        _reader = reader;
        PriceAlerts = priceAlerts;
        _accountId = selected.AccountId;
        HasAccount = !string.IsNullOrEmpty(selected.AccountId);
        ShowMoreTradesCommand = new RelayCommand(() =>
        {
            _tradesShown += TradesPageSize;
            ApplyTradeView();
        });

        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom()
    {
        _dataChanges.Dispose();
        PriceAlerts.OnNavigatedFrom();
    }

    /// <summary>The Price alerts tab. Alerts are about trading post prices but need no account.</summary>
    public PriceAlertsViewModel PriceAlerts { get; }

    public bool HasAccount { get; }

    public ObservableCollection<TradingPostOrderRow> Orders { get; } = new();

    /// <summary>The filtered trades currently shown (the first pages of them).</summary>
    public ObservableCollection<TradingPostTradeRow> Trades { get; } = new();

    public bool HasOrders => Orders.Count > 0;

    public bool NoOrders => _loaded && Orders.Count == 0;

    public bool NoTrades => _loaded && Trades.Count == 0;

    /// <summary>E.g. "2 of 5 orders undercut or outbid", or empty when none are.</summary>
    public string OrdersSummary
    {
        get
        {
            int beaten = Orders.Count(o => o.IsBeaten);
            return beaten == 0
                ? $"{Orders.Count:N0} open {(Orders.Count == 1 ? "order" : "orders")}, all at the best price."
                : $"{beaten:N0} of {Orders.Count:N0} open {(Orders.Count == 1 ? "order" : "orders")} undercut or outbid.";
        }
    }

    public IReadOnlyList<string> TradeFilterOptions { get; } = new[] { "All", "Buys", "Sells" };

    public int SelectedTradeFilterIndex
    {
        get => _selectedTradeFilterIndex;
        set
        {
            if (SetProperty(ref _selectedTradeFilterIndex, value))
            {
                _tradesShown = TradesPageSize;
                ApplyTradeView();
            }
        }
    }

    public bool HasMoreTrades { get; private set; }

    /// <summary>E.g. "Showing 200 of 1,203 trades".</summary>
    public string TradesSummary { get; private set; } = "";

    public RelayCommand ShowMoreTradesCommand { get; }

    private IEnumerable<TradingPostTradeRow> FilteredTrades => SelectedTradeFilterIndex switch
    {
        1 => _allTrades.Where(t => !t.IsSell),
        2 => _allTrades.Where(t => t.IsSell),
        _ => _allTrades,
    };

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            (List<TradingPostOrderRow> orders, List<TradingPostTradeRow> trades) = await Task.Run(() =>
                accountId is null
                    ? (new List<TradingPostOrderRow>(), new List<TradingPostTradeRow>())
                    : (reader.GetTradingPostOrders(accountId), reader.GetTradingPostTrades(accountId)));

            _loaded = true;
            Orders.ReplaceWith(orders);
            _allTrades = trades;
            OnPropertyChanged(nameof(HasOrders));
            OnPropertyChanged(nameof(NoOrders));
            OnPropertyChanged(nameof(OrdersSummary));
            ApplyTradeView();
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void ApplyTradeView()
    {
        List<TradingPostTradeRow> filtered = FilteredTrades.ToList();
        Trades.ReplaceWith(filtered.Take(_tradesShown));
        HasMoreTrades = filtered.Count > Trades.Count;
        TradesSummary = filtered.Count == 0 ? "" : $"Showing {Trades.Count:N0} of {filtered.Count:N0} trades";
        OnPropertyChanged(nameof(HasMoreTrades));
        OnPropertyChanged(nameof(TradesSummary));
        OnPropertyChanged(nameof(NoTrades));
    }
}
