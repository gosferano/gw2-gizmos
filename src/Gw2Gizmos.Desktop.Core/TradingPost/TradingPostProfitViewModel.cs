using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A period the Profit page can show, from its start until now.</summary>
public sealed record TradingPostProfitPeriodOption(string Label, Func<DateTimeOffset, DateTimeOffset> Start);

/// <summary>An item's row on the Profit page.</summary>
public sealed record TradingPostItemProfitRow(TradingPostItemProfit Item)
{
    public int ItemId => Item.ItemId;

    public string Name => Item.Name;

    /// <summary>E.g. "Sold 25".</summary>
    public string SoldText => $"Sold {Item.Sold:N0}";

    /// <summary>E.g. "25% margin"; empty for an item not bought here.</summary>
    public string MarginText =>
        Item.Margin is { } margin ? $"{margin.ToString("P0", CultureInfo.CurrentCulture)} margin" : "";

    public bool HasMargin => Item.Margin is not null;

    public long ProfitCopper => Item.Profit;

    /// <summary>A traded item partly sold without a matching purchase: that part's proceeds are income.</summary>
    public bool HasIncome => HasProfit && Item.Income > 0;

    public long IncomeCopper => Item.Income;

    /// <summary>Profit is shown when any unit was matched; otherwise the row is all income.</summary>
    public bool HasProfit => Item.Cost > 0;
}

/// <summary>
/// Backs the Trading post › Profit page: realized profit from the account's own trades over a chosen period — sales
/// matched to earlier purchases, after fees — with the income from sales of items not bought here, the fees paid, and
/// each item's share, most profitable first. Refreshes after syncs.
/// </summary>
public sealed class TradingPostProfitViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private IReadOnlyList<TradingPostSale> _sales = [];
    private TradingPostProfitPeriodOption _period;
    private TradingPostPeriod _shown = TradingPostPeriod.Empty;
    private bool _loaded;

    public TradingPostProfitViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges
    )
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = TradingPostBreadcrumbs.Section("Profit");
        _period = Periods[1];
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    public IReadOnlyList<TradingPostProfitPeriodOption> Periods { get; } =
    [
        new("This week", TradingPostPeriods.WeekStart),
        new("This month", TradingPostPeriods.MonthStart),
        new("Last 30 days", now => now - TimeSpan.FromDays(30)),
        new("This year", TradingPostPeriods.YearStart),
        new("All time", _ => DateTimeOffset.MinValue),
    ];

    public TradingPostProfitPeriodOption Period
    {
        get => _period;
        set
        {
            if (value is not null && SetProperty(ref _period, value))
            {
                Apply();
            }
        }
    }

    /// <summary>Profit on units sold that were bought here, after fees.</summary>
    public long ProfitCopper => _shown.Profit;

    /// <summary>Proceeds from units sold that weren't bought here (drops, crafts, bought before recording).</summary>
    public long IncomeCopper => _shown.Income;

    /// <summary>Listing and exchange fees on the sales.</summary>
    public long FeesCopper => _shown.Fees;

    /// <summary>Items bought here and sold, most profitable first (losses last).</summary>
    public IReadOnlyList<TradingPostItemProfitRow> Traded { get; private set; } = [];

    /// <summary>Items sold that weren't bought here, by income.</summary>
    public IReadOnlyList<TradingPostItemProfitRow> IncomeOnly { get; private set; } = [];

    public bool HasTraded => Traded.Count > 0;

    public bool HasIncomeOnly => IncomeOnly.Count > 0;

    /// <summary>The income from <see cref="IncomeOnly"/>, for its heading.</summary>
    public long IncomeOnlyCopper => IncomeOnly.Sum(row => row.IncomeCopper);

    public bool NoSales => _loaded && Traded.Count == 0 && IncomeOnly.Count == 0;

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            _sales = await Task.Run(() => TradingPostLedger.Sales(_reader.GetTradingPostTrades(accountId)));
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
        _shown = TradingPostLedger.Period(_sales, _period.Start(DateTimeOffset.UtcNow));
        // Profit and income are different things: what trading made, and what selling things got some other way
        // brought in. Each gets its own list, so a loss sits with the other trades rather than below the income.
        Traded = _shown.Items.Where(i => i.Cost > 0)
            .OrderByDescending(i => i.Profit)
            .Select(i => new TradingPostItemProfitRow(i))
            .ToList();
        IncomeOnly = _shown.Items.Where(i => i.Cost == 0)
            .OrderByDescending(i => i.Income)
            .Select(i => new TradingPostItemProfitRow(i))
            .ToList();
        OnPropertyChanged(nameof(Traded));
        OnPropertyChanged(nameof(IncomeOnly));
        OnPropertyChanged(nameof(HasTraded));
        OnPropertyChanged(nameof(HasIncomeOnly));
        OnPropertyChanged(nameof(IncomeOnlyCopper));
        OnPropertyChanged(nameof(ProfitCopper));
        OnPropertyChanged(nameof(IncomeCopper));
        OnPropertyChanged(nameof(FeesCopper));
        OnPropertyChanged(nameof(NoSales));
    }
}
