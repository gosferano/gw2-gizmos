using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Trading post › Orders page: the account's open buy and sell orders, beaten ones first, each with how
/// long it has been open; stale ones (old and beaten) are marked, and can be shown alone. Refreshes after syncs.
/// </summary>
public sealed class TradingPostOrdersViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private List<TradingPostOrderRow> _all = [];
    private bool _staleOnly;
    private bool _loaded;

    public TradingPostOrdersViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges
    )
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = TradingPostBreadcrumbs.Section("Orders");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    public ObservableCollection<TradingPostOrderRow> Orders { get; } = new();

    /// <summary>Only the stale orders: open a week or more, and beaten by a better price.</summary>
    public bool StaleOnly
    {
        get => _staleOnly;
        set
        {
            if (SetProperty(ref _staleOnly, value))
            {
                Apply();
            }
        }
    }

    public bool NoOrders => _loaded && _all.Count == 0;

    public bool HasOrders => _all.Count > 0;

    /// <summary>E.g. "2 of 5 open orders undercut or outbid, 1 stale".</summary>
    public string Summary
    {
        get
        {
            int beaten = _all.Count(o => o.IsBeaten);
            int stale = _all.Count(o => o.IsStale);
            string orders = $"{_all.Count:N0} open {(_all.Count == 1 ? "order" : "orders")}";
            string standing = beaten == 0
                ? $"{orders}, all at the best price"
                : $"{beaten:N0} of {orders} undercut or outbid";
            return stale == 0 ? $"{standing}." : $"{standing}, {stale:N0} stale.";
        }
    }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            _all = await Task.Run(() => _reader.GetTradingPostOrders(accountId));
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
        Orders.ReplaceWith(_staleOnly ? _all.Where(o => o.IsStale) : _all);
        OnPropertyChanged(nameof(NoOrders));
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(Summary));
    }
}
