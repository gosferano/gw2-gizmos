using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the account's Value page: a chart of its estimated value over a chosen range (total plus where it's held),
/// the change over that range, and a breakdown of the latest point. Refreshes as the worker records new points.
/// </summary>
public sealed class AccountValueViewModel : ViewModelBase, INavigationAware
{
    private static readonly TimeSpan?[] Ranges =
        { TimeSpan.FromDays(7), TimeSpan.FromDays(30), TimeSpan.FromDays(90), null };

    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private List<AccountValuePoint> _all = new();
    private IReadOnlyList<AccountValuePoint> _points = Array.Empty<AccountValuePoint>();
    private int _selectedRangeIndex = 1;
    private bool _loaded;
    private bool _hasPriceHistory = true;

    public AccountValueViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = selected.AccountId;
        Breadcrumbs = AccountBreadcrumbs.Section("Value");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(changes =>
        {
            if ((changes & (DataChanges.AccountValue | DataChanges.Catalog)) != 0)
            {
                _ = LoadAsync();
            }
        });
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    /// <summary>Raised when <see cref="Points"/> changes, for the chart to redraw.</summary>
    public event EventHandler? PointsChanged;

    /// <summary>The points inside the selected range, oldest first.</summary>
    public IReadOnlyList<AccountValuePoint> Points => _points;

    /// <summary>
    /// The span the chart's time axis shows: the whole selected range up to now (so sparse data reads as sparse),
    /// or from the first point for "All".
    /// </summary>
    public DateTimeOffset RangeStart { get; private set; }

    public DateTimeOffset RangeEnd { get; private set; }

    public bool HasData => _points.Count > 0;

    public bool NoData => _loaded && _all.Count == 0;

    public string[] RangeOptions { get; } = { "7 days", "30 days", "90 days", "All" };

    /// <summary>The chosen range, one of <see cref="RangeOptions"/>. Bound by item rather than index: a combo box
    /// whose items arrive after its index would otherwise push its own first item back.</summary>
    public string SelectedRange
    {
        get => RangeOptions[_selectedRangeIndex];
        set
        {
            int index = Math.Max(0, Array.IndexOf(RangeOptions, value));
            if (index != _selectedRangeIndex)
            {
                _selectedRangeIndex = index;
                OnPropertyChanged();
                ApplyRange();
            }
        }
    }

    /// <summary>The latest total, in copper.</summary>
    public long CurrentTotal => _points.Count > 0 ? _points[^1].Total : 0;

    /// <summary>The total's change from the first point in range to the latest, in copper.</summary>
    public long RangeChange => _points.Count > 0 ? _points[^1].Total - _points[0].Total : 0;

    /// <summary>E.g. "+4.2% since 2026-09-16", or empty with a single point.</summary>
    public string RangeChangeText
    {
        get
        {
            if (_points.Count < 2)
            {
                return "";
            }

            long first = _points[0].Total;
            string percent = first > 0 ? $"{(double)RangeChange / first:+0.0%;-0.0%;0.0%} " : "";
            return $"{percent}since {_points[0].Time.LocalDateTime:yyyy-MM-dd}";
        }
    }

    /// <summary>The latest point by where the value is held, each with its change over the range.</summary>
    public ObservableCollection<AccountValueCategoryRow> Breakdown { get; } = new();

    /// <summary>Without price history there are no trading-post prices, so items fall back to vendor prices.</summary>
    public bool ShowPriceNote => _loaded && !_hasPriceHistory;

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            (List<AccountValuePoint> points, bool hasPrices) = await Task.Run(() =>
                accountId is null
                    ? (new List<AccountValuePoint>(), true)
                    : (reader.GetAccountValueHistory(accountId), reader.GetPriceHistoryCount() > 0));

            _all = points;
            _hasPriceHistory = hasPrices;
            _loaded = true;
            OnPropertyChanged(nameof(NoData));
            OnPropertyChanged(nameof(ShowPriceNote));
            ApplyRange();
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void ApplyRange()
    {
        TimeSpan? range = Ranges[_selectedRangeIndex];
        DateTimeOffset from = range is { } span ? DateTimeOffset.UtcNow - span : DateTimeOffset.MinValue;
        _points = _all.Where(p => p.Time >= from).ToList();
        RangeEnd = DateTimeOffset.UtcNow;
        RangeStart = range is not null ? from : _points.Count > 0 ? _points[0].Time : RangeEnd.AddDays(-1);

        AccountValuePoint? first = _points.Count > 0 ? _points[0] : null;
        AccountValuePoint? last = _points.Count > 0 ? _points[^1] : null;
        Breakdown.ReplaceWith(AccountValueCategories.All
            .Select(c => new AccountValueCategoryRow(
                c.Name, c.Color, last is null ? 0 : c.Value(last), last is null ? 0 : c.Value(last) - c.Value(first!)))
            .Where(r => r.Value != 0 || r.Change != 0));

        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(CurrentTotal));
        OnPropertyChanged(nameof(RangeChange));
        OnPropertyChanged(nameof(RangeChangeText));
        PointsChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Where an account's value is held, with the colour its line and legend dot are drawn in.</summary>
public sealed record AccountValueCategory(string Name, uint Color, Func<AccountValuePoint, long> Value);

/// <summary>The value categories, in display order; the total is drawn on its own.</summary>
public static class AccountValueCategories
{
    public const uint TotalColor = 0xFF7E57C2;

    public static IReadOnlyList<AccountValueCategory> All { get; } = new AccountValueCategory[]
    {
        new("Coin", 0xFFFFC107, p => p.Coin),
        new("Material storage", 0xFF66BB6A, p => p.Materials),
        new("Bank", 0xFF42A5F5, p => p.Bank),
        new("Shared inventory", 0xFFAB47BC, p => p.SharedInventory),
        new("Characters", 0xFFFF7043, p => p.Characters),
        new("Trading post", 0xFF26A69A, p => p.TradingPost),
    };
}

/// <summary>One category in the breakdown: its latest value and its change over the selected range (copper).</summary>
public sealed record AccountValueCategoryRow(string Name, uint Color, long Value, long Change);
