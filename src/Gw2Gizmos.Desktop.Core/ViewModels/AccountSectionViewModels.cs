using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Per-section Account view-models. Each is transient, so navigating to a sub-page reloads that section for
/// the selected account (<see cref="SelectedAccountService"/>, falling back to the most-recently-synced one).
/// The whole read runs in <see cref="Task.Run"/> so navigation never blocks the UI thread; the populated
/// collection is then filled on the resumed UI thread. <see cref="Breadcrumbs"/> carries the account name. Each
/// refreshes in place after an account sync (a sync also catches items moved between slots, which holdings don't).
/// </summary>
public sealed class WalletViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;

    public WalletViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = selected.AccountId;
        Breadcrumbs = AccountBreadcrumbs.Section("Wallet");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<WalletRow> Wallet { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            List<WalletRow> rows = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<WalletRow>() : reader.GetWallet(id);
            });

            Wallet.ReplaceWith(rows);
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class MaterialStorageViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;

    public MaterialStorageViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = selected.AccountId;
        Breadcrumbs = AccountBreadcrumbs.Section("Material storage");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<MaterialCategoryRow> Categories { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            List<MaterialCategoryView> categories = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<MaterialCategoryView>() : reader.GetMaterials(id);
            });

            // Matched by name and updated in place (only changed cells change), so expanded categories stay open.
            Categories.ReplaceWith(
                categories.Select(c => new MaterialCategoryRow(c)),
                row => row.Name,
                (row, latest) => row.Items.ReplaceWith(latest.Items));
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class BankViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;

    public BankViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = selected.AccountId;
        Breadcrumbs = AccountBreadcrumbs.Section("Bank");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<SlotRow> Slots { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<SlotRow>() : reader.GetSlots(id, AccountContainer.Bank);
            });

            Slots.ReplaceWith(slots);
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class SharedInventoryViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;

    public SharedInventoryViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = selected.AccountId;
        Breadcrumbs = AccountBreadcrumbs.Section("Shared inventory");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<SlotRow> Slots { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync()
    {
        AccountReader reader = _reader;
        string? accountId = _accountId;
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<SlotRow>() : reader.GetSlots(id, AccountContainer.SharedInventory);
            });

            Slots.ReplaceWith(slots);
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

/// <summary>The data changes the account and character pages show: a finished sync, or new holdings.</summary>
internal static class AccountPageRefresh
{
    public static Action<DataChanges> When(Action reload) => changes =>
    {
        if ((changes & (DataChanges.AccountSync | DataChanges.Holdings)) != 0)
        {
            reload();
        }
    };
}

/// <summary>A material category on the page, whose cells refresh in place (so its expander keeps its state).</summary>
public sealed class MaterialCategoryRow
{
    public MaterialCategoryRow(MaterialCategoryView category)
    {
        Name = category.Name;
        Items = new ObservableCollection<SlotRow>(category.Items);
    }

    public string Name { get; }

    public ObservableCollection<SlotRow> Items { get; }
}
