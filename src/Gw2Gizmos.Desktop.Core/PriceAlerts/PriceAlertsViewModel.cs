using System;
using System.Collections.ObjectModel;
using System.Linq;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Price alerts page: every alert with its condition, the item's current price and how far it is from
/// triggering (or when it triggered), to edit or remove. Follows the store, so it updates as prices are checked.
/// </summary>
public sealed class PriceAlertsViewModel : ViewModelBase, INavigationAware
{
    private readonly PriceAlertStore _store;
    private readonly PriceAlertEditing _editing;
    private readonly IUiDispatcher _dispatcher;

    public PriceAlertsViewModel(PriceAlertStore store, PriceAlertEditing editing, IUiDispatcher dispatcher)
    {
        _store = store;
        _editing = editing;
        _dispatcher = dispatcher;
        EditCommand = new RelayCommand<PriceAlertRow>(row => _ = row is null ? null : _editing.EditAsync(row.Alert));
        RemoveCommand = new RelayCommand<PriceAlertRow>(row =>
        {
            if (row is not null)
            {
                _store.Remove(row.Alert.Id);
            }
        });

        Load();
        _store.Changed += OnStoreChanged;
    }

    public void OnNavigatedFrom() => _store.Changed -= OnStoreChanged;

    public BreadcrumbEntry[] Breadcrumbs { get; } = TradingPostBreadcrumbs.Section("Price alerts");

    public ObservableCollection<PriceAlertRow> Alerts { get; } = new();

    public bool NoAlerts => Alerts.Count == 0;

    public RelayCommand<PriceAlertRow> EditCommand { get; }

    public RelayCommand<PriceAlertRow> RemoveCommand { get; }

    // The price check saves from a background thread.
    private void OnStoreChanged() => _dispatcher.Post(Load);

    private void Load()
    {
        // Triggered alerts first (they need attention), then by item.
        Alerts.ReplaceWith(_store.All
            .OrderByDescending(a => a.IsTriggered)
            .ThenBy(a => a.ItemName)
            .Select(a => new PriceAlertRow(a)));
        OnPropertyChanged(nameof(NoAlerts));
    }
}

/// <summary>One alert as listed: its condition, the item's current price, and its standing.</summary>
public sealed record PriceAlertRow(PriceAlert Alert)
{
    public int ItemId => Alert.ItemId;

    public string ItemName => Alert.ItemName;

    /// <summary>E.g. "Sell price below", followed in the view by the threshold.</summary>
    public string Condition => PriceAlerts.Condition(Alert);

    public int PriceCopper => Alert.PriceCopper;

    public string RepeatText => Alert.Repeat ? "repeats" : "once";

    public int? CurrentPrice => Alert.LastPrice;

    public bool HasCurrentPrice => Alert.LastPrice is not null;

    /// <summary>How far the current price is from triggering, in copper; null when triggered or unknown.</summary>
    public long? Distance => Alert.IsTriggered ? null : PriceAlerts.Distance(Alert);

    public bool HasDistance => Distance is not null;

    public bool IsTriggered => Alert.IsTriggered;

    /// <summary>The standing when there's no distance to show, e.g. "Triggered 14:05", "No buy orders".</summary>
    public string Status =>
        Alert.IsTriggered
            ? $"Triggered {FormatTime(Alert.TriggeredAtUtc)}"
            : Alert.LastCheckedUtc is null
                ? "Checking…"
                : Alert.LastPrice is null
                    ? Alert.Side == PriceAlertSide.Buy ? "No buy orders" : "No sell listings"
                    : "";

    public bool HasStatus => Status.Length > 0;

    // By what's shown: the time of the last check changes on every price check even when nothing on screen does, so
    // only whether it has been checked counts — otherwise every row would be replaced each check.
    public bool Equals(PriceAlertRow? other) =>
        other is not null && Shown(Alert) == Shown(other.Alert);

    public override int GetHashCode() => Shown(Alert).GetHashCode();

    private static PriceAlert Shown(PriceAlert alert) =>
        alert with { LastCheckedUtc = alert.LastCheckedUtc is null ? null : DateTimeOffset.MinValue };

    private static string FormatTime(DateTimeOffset? utc)
    {
        if (utc is not { } time)
        {
            return "";
        }

        DateTime local = time.LocalDateTime;
        return local.Date == DateTime.Today ? local.ToString("HH:mm") : local.ToString("yyyy-MM-dd HH:mm");
    }
}
