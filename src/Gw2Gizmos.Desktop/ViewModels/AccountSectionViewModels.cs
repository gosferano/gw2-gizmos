using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Per-section Account view-models. Each is transient, so navigating to a sub-page reloads that section for
/// the selected account (<see cref="SelectedAccountService"/>, falling back to the most-recently-synced one).
/// The whole read runs in <see cref="Task.Run"/> so navigation never blocks the UI thread; the populated
/// collection is then filled on the resumed UI thread. <see cref="Breadcrumbs"/> carries the account name.
/// </summary>
public sealed class WalletViewModel : ViewModelBase
{
    public WalletViewModel(AccountReader reader, SelectedAccountService selected)
    {
        Breadcrumbs = AccountBreadcrumbs.Section("Wallet");
        _ = LoadAsync(reader, selected.AccountId);
    }

    public ObservableCollection<WalletRow> Wallet { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync(AccountReader reader, string? accountId)
    {
        try
        {
            List<WalletRow> rows = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<WalletRow>() : reader.GetWallet(id);
            });

            foreach (WalletRow row in rows)
            {
                Wallet.Add(row);
            }
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class MaterialStorageViewModel : ViewModelBase
{
    public MaterialStorageViewModel(AccountReader reader, SelectedAccountService selected)
    {
        Breadcrumbs = AccountBreadcrumbs.Section("Material storage");
        _ = LoadAsync(reader, selected.AccountId);
    }

    public ObservableCollection<MaterialCategoryView> Categories { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync(AccountReader reader, string? accountId)
    {
        try
        {
            List<MaterialCategoryView> categories = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<MaterialCategoryView>() : reader.GetMaterials(id);
            });

            foreach (MaterialCategoryView category in categories)
            {
                Categories.Add(category);
            }
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class BankViewModel : ViewModelBase
{
    public BankViewModel(AccountReader reader, SelectedAccountService selected)
    {
        Breadcrumbs = AccountBreadcrumbs.Section("Bank");
        _ = LoadAsync(reader, selected.AccountId);
    }

    public ObservableCollection<SlotRow> Slots { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync(AccountReader reader, string? accountId)
    {
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<SlotRow>() : reader.GetSlots(id, AccountContainer.Bank);
            });

            foreach (SlotRow slot in slots)
            {
                Slots.Add(slot);
            }
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}

public sealed class SharedInventoryViewModel : ViewModelBase
{
    public SharedInventoryViewModel(AccountReader reader, SelectedAccountService selected)
    {
        Breadcrumbs = AccountBreadcrumbs.Section("Shared inventory");
        _ = LoadAsync(reader, selected.AccountId);
    }

    public ObservableCollection<SlotRow> Slots { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync(AccountReader reader, string? accountId)
    {
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<SlotRow>() : reader.GetSlots(id, AccountContainer.SharedInventory);
            });

            foreach (SlotRow slot in slots)
            {
                Slots.Add(slot);
            }
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}
