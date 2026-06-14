using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Per-section Account view-models. Each is transient, so navigating to a sub-page reloads that section's
/// current data off the UI thread. They share the read-only <see cref="AccountReader"/>. The whole read —
/// resolving the current account and querying its data — runs in <see cref="Task.Run"/> so navigation never
/// blocks the UI thread (the first DB touch also pays EF's cold-start cost); the populated collection is then
/// filled on the resumed UI thread.
/// </summary>
public sealed class WalletViewModel : ViewModelBase
{
    public WalletViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<WalletRow> Wallet { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            List<WalletRow> rows = await Task.Run(() =>
            {
                AccountInfo? account = reader.GetCurrentAccount();
                return account is null ? new List<WalletRow>() : reader.GetWallet(account.Id);
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
    public MaterialStorageViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<MaterialCategoryView> Categories { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            List<MaterialCategoryView> categories = await Task.Run(() =>
            {
                AccountInfo? account = reader.GetCurrentAccount();
                return account is null ? new List<MaterialCategoryView>() : reader.GetMaterials(account.Id);
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
    public BankViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<SlotRow> Slots { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                AccountInfo? account = reader.GetCurrentAccount();
                return account is null ? new List<SlotRow>() : reader.GetSlots(account.Id, AccountContainer.Bank);
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
    public SharedInventoryViewModel(AccountReader reader) => _ = LoadAsync(reader);

    public ObservableCollection<SlotRow> Slots { get; } = new();

    private async Task LoadAsync(AccountReader reader)
    {
        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                AccountInfo? account = reader.GetCurrentAccount();
                return account is null
                    ? new List<SlotRow>()
                    : reader.GetSlots(account.Id, AccountContainer.SharedInventory);
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
