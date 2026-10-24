using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Trading post page: a hub of its sections — orders, history, price alerts and profit — each with a
/// summary of what's in it, opening its own page. Refreshes after account syncs and as price alerts are checked.
/// </summary>
public sealed class TradingPostViewModel : ViewModelBase, INavigationAware
{
    private static readonly TimeSpan HistorySpan = TimeSpan.FromDays(30);

    private readonly AccountReader _reader;
    private readonly PriceAlertStore _alerts;
    private readonly IUiDispatcher _dispatcher;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private string _ordersSummary = "";
    private long _tiedUpCopper;
    private string _historySummary = "";
    private long _historyNetCopper;
    private string _alertsSummary = "";
    private string _profitSummary = "";
    private long _monthProfitCopper;

    public TradingPostViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges,
        PriceAlertStore alerts,
        INavigationService navigation,
        IUiDispatcher dispatcher
    )
    {
        _reader = reader;
        _alerts = alerts;
        _dispatcher = dispatcher;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        OpenCommand = new RelayCommand<AppPage>(page => navigation.NavigateTo(page));

        SummariseAlerts();
        _alerts.Changed += OnAlertsChanged;
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom()
    {
        _dataChanges.Dispose();
        _alerts.Changed -= OnAlertsChanged;
    }

    public bool HasAccount { get; }

    /// <summary>Opens a section's page.</summary>
    public RelayCommand<AppPage> OpenCommand { get; }

    /// <summary>E.g. "12 buying · 8 selling · 3 outbid or undercut · 2 stale".</summary>
    public string OrdersSummary
    {
        get => _ordersSummary;
        private set => SetProperty(ref _ordersSummary, value);
    }

    /// <summary>The coin and item value the open orders tie up.</summary>
    public long TiedUpCopper
    {
        get => _tiedUpCopper;
        private set => SetProperty(ref _tiedUpCopper, value);
    }

    /// <summary>E.g. "143 bought · 97 sold in the last 30 days · last trade 2h ago".</summary>
    public string HistorySummary
    {
        get => _historySummary;
        private set => SetProperty(ref _historySummary, value);
    }

    /// <summary>What the last 30 days' trades did to the wallet: sales' proceeds less purchases.</summary>
    public long HistoryNetCopper
    {
        get => _historyNetCopper;
        private set => SetProperty(ref _historyNetCopper, value);
    }

    /// <summary>E.g. "5 alerts · 1 triggered · closest: Mystic Coin, 2% from its target".</summary>
    public string AlertsSummary
    {
        get => _alertsSummary;
        private set => SetProperty(ref _alertsSummary, value);
    }

    /// <summary>E.g. "This week +18g · fees 12g this month · best: Glob of Ectoplasm".</summary>
    public string ProfitSummary
    {
        get => _profitSummary;
        private set => SetProperty(ref _profitSummary, value);
    }

    /// <summary>Realized profit this month.</summary>
    public long MonthProfitCopper
    {
        get => _monthProfitCopper;
        private set => SetProperty(ref _monthProfitCopper, value);
    }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            const string needsAccount = "Needs an account: add an API key on the API keys page";
            OrdersSummary = needsAccount;
            HistorySummary = needsAccount;
            ProfitSummary = needsAccount;
            return;
        }

        try
        {
            (List<TradingPostOrderRow> orders, List<TradingPostTradeRow> trades) = await Task.Run(() =>
                (_reader.GetTradingPostOrders(accountId), _reader.GetTradingPostTrades(accountId)));
            Summarise(orders, trades, DateTimeOffset.UtcNow);
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void Summarise(List<TradingPostOrderRow> orders, List<TradingPostTradeRow> trades, DateTimeOffset now)
    {
        int buying = orders.Count(o => o.IsBuy);
        int beaten = orders.Count(o => o.IsBeaten);
        int stale = orders.Count(o => o.IsStale);
        OrdersSummary = orders.Count == 0
            ? "No open orders"
            : Join(
                $"{Number(buying)} buying",
                $"{Number(orders.Count - buying)} selling",
                beaten > 0 ? $"{Number(beaten)} outbid or undercut" : null,
                stale > 0 ? $"{Number(stale)} stale" : null);
        TiedUpCopper = orders.Sum(o => o.TiedUpCopper);

        List<TradingPostTradeRow> recent = trades.Where(t => t.PurchasedUtc >= now - HistorySpan).ToList();
        IReadOnlyList<TradingPostSale> sales = TradingPostLedger.Sales(trades);
        long spent = recent.Where(t => !t.IsSell).Sum(t => t.TotalCopper);
        long received = sales.Where(s => s.SoldUtc >= now - HistorySpan).Sum(s => s.Proceeds);
        HistoryNetCopper = received - spent;
        HistorySummary = trades.Count == 0
            ? "No completed trades yet"
            : Join(
                $"{Number(recent.Count(t => !t.IsSell))} bought",
                $"{Number(recent.Count(t => t.IsSell))} sold in the last 30 days",
                $"last trade {RelativeTime.Format(trades.Max(t => t.PurchasedUtc))}");

        TradingPostPeriod month = TradingPostLedger.Period(sales, TradingPostPeriods.MonthStart(now));
        TradingPostPeriod week = TradingPostLedger.Period(sales, TradingPostPeriods.WeekStart(now));
        MonthProfitCopper = month.Profit;
        ProfitSummary = sales.Count == 0
            ? "No sales yet"
            : Join($"This week {Coin.Format(week.Profit)}", $"fees {Coin.Format(month.Fees)} this month",
                month.Best is { } best ? $"best: {best.Name}" : null);
    }

    // The price check saves from a background thread.
    private void OnAlertsChanged() => _dispatcher.Post(SummariseAlerts);

    private void SummariseAlerts() => AlertsSummary = PriceAlerts.Summary(_alerts.All);

    private static string Join(params string?[] parts) => string.Join(" · ", parts.Where(p => p is not null));

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
