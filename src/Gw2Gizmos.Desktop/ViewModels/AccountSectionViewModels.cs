using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Per-section Account view-models. Each is transient, so navigating to a sub-page reloads that section's
/// current data off the UI thread. They share the read-only <see cref="AccountReader"/>.
/// </summary>
public sealed class WalletViewModel : ViewModelBase
{
    public WalletViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<WalletRow> Wallet { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            AccountInfo? account = reader.GetCurrentAccount();
            if (account is null)
            {
                return;
            }

            foreach (WalletRow row in await Task.Run(() => reader.GetWallet(account.Id)))
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
    public MaterialStorageViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<MaterialCategoryView> Categories { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            AccountInfo? account = reader.GetCurrentAccount();
            if (account is null)
            {
                return;
            }

            foreach (MaterialCategoryView category in await Task.Run(() => reader.GetMaterials(account.Id)))
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
    public BankViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<SlotRow> Slots { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            AccountInfo? account = reader.GetCurrentAccount();
            if (account is null)
            {
                return;
            }

            foreach (SlotRow slot in await Task.Run(() => reader.GetSlots(account.Id, AccountContainer.Bank)))
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
    public SharedInventoryViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<SlotRow> Slots { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            AccountInfo? account = reader.GetCurrentAccount();
            if (account is null)
            {
                return;
            }

            foreach (SlotRow slot in await Task.Run(() => reader.GetSlots(account.Id, AccountContainer.SharedInventory)))
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
