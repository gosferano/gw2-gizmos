using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Settings → Stored data section: delete locally-stored data per type, per account (plus global price
/// history). The worker is the DB's sole writer, so a delete is queued to it (<see cref="DeleteRequestStore"/>)
/// rather than run here; counts re-read a few seconds later once the worker has processed it.
/// </summary>
public sealed class StoredDataViewModel : ViewModelBase
{
    // Worker config cache (~3s) + delete-poll (~3s); wait past both before re-reading counts. A large delete may
    // still be running, in which case the count settles on the next visit to the page.
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromSeconds(7);

    private readonly AccountReader _reader;
    private readonly DeleteRequestStore _deletes;
    private bool _hasAccounts;

    public StoredDataViewModel(AccountReader reader, DeleteRequestStore deletes)
    {
        _reader = reader;
        _deletes = deletes;
        var priceHistory = new StoredDataItem(DeletableData.PriceHistory, "Price history");
        priceHistory.Delete = new RelayCommand(() => DeleteGlobal(priceHistory, "price history (all accounts)"));
        PriceHistory = priceHistory;

        _ = LoadAsync();
    }

    public BreadcrumbEntry[] Breadcrumbs { get; } = new[]
    {
        new BreadcrumbEntry { Title = "Settings", Target = typeof(SettingsPage) },
        new BreadcrumbEntry { Title = "Stored data" },
    };

    /// <summary>Global, non-account data.</summary>
    public StoredDataItem PriceHistory { get; }

    /// <summary>One group per synced account, each with its deletable types.</summary>
    public ObservableCollection<AccountStoredData> Accounts { get; } = new();

    public bool HasAccounts
    {
        get => _hasAccounts;
        private set => SetProperty(ref _hasAccounts, value);
    }

    private async Task LoadAsync()
    {
        try
        {
            List<AccountInfo> accounts = await Task.Run(() => _reader.GetAccounts());
            PriceHistory.Count = await Task.Run(() => _reader.GetPriceHistoryCount());

            foreach (AccountInfo account in accounts)
            {
                Dictionary<string, int> counts = await Task.Run(() => _reader.GetAccountDataCounts(account.Id));
                var group = new AccountStoredData(account.Id, account.Name);
                foreach (DeletableDataType type in DeletableData.AccountTypes)
                {
                    var item = new StoredDataItem(type.Key, type.Display) { Count = counts.GetValueOrDefault(type.Key) };
                    item.Delete = new RelayCommand(() => DeleteAccountType(account, item));
                    group.Types.Add(item);
                }

                group.DeleteAll = new RelayCommand(() => DeleteAll(group));
                Accounts.Add(group);
            }

            HasAccounts = Accounts.Count > 0;
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    private void DeleteGlobal(StoredDataItem item, string what)
    {
        if (!Confirm(what))
        {
            return;
        }

        _deletes.Enqueue(item.TypeKey, accountId: null);
        item.Busy = true;
        _ = RefreshPriceHistoryAsync();
    }

    private void DeleteAccountType(AccountInfo account, StoredDataItem item)
    {
        if (!Confirm($"{item.Display} for {account.Name}"))
        {
            return;
        }

        _deletes.Enqueue(item.TypeKey, account.Id);
        item.Busy = true;
        _ = RefreshAccountAsync(account.Id);
    }

    private void DeleteAll(AccountStoredData group)
    {
        if (!Confirm($"ALL data for {group.AccountName}"))
        {
            return;
        }

        _deletes.Enqueue(DeletableData.AllForAccount, group.AccountId);
        foreach (StoredDataItem item in group.Types)
        {
            item.Busy = true;
        }

        _ = RefreshAccountAsync(group.AccountId);
    }

    private async Task RefreshPriceHistoryAsync()
    {
        await Task.Delay(RefreshDelay);
        PriceHistory.Count = await Task.Run(() => _reader.GetPriceHistoryCount());
        PriceHistory.Busy = false;
    }

    private async Task RefreshAccountAsync(string accountId)
    {
        await Task.Delay(RefreshDelay);
        Dictionary<string, int> counts = await Task.Run(() => _reader.GetAccountDataCounts(accountId));
        AccountStoredData? group = Accounts.FirstOrDefault(a => a.AccountId == accountId);
        if (group is null)
        {
            return;
        }

        foreach (StoredDataItem item in group.Types)
        {
            item.Count = counts.GetValueOrDefault(item.TypeKey);
            item.Busy = false;
        }
    }

    private static bool Confirm(string what) =>
        MessageBox.Show(
            $"Delete {what}?\n\nThis clears the stored rows now. Current data re-syncs on the next update "
                + "(play sessions and price history don't come back).",
            "Delete stored data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        ) == MessageBoxResult.Yes;
}

/// <summary>One deletable data category: its label, current row count, and delete command.</summary>
public sealed class StoredDataItem : ViewModelBase
{
    private int _count;
    private bool _busy;

    public StoredDataItem(string typeKey, string display)
    {
        TypeKey = typeKey;
        Display = display;
    }

    public string TypeKey { get; }

    public string Display { get; }

    public RelayCommand Delete { get; set; } = null!;

    public int Count
    {
        get => _count;
        set
        {
            if (SetProperty(ref _count, value))
            {
                OnPropertyChanged(nameof(CountDisplay));
            }
        }
    }

    /// <summary>True while a delete is queued and we're waiting for the worker to process it.</summary>
    public bool Busy
    {
        get => _busy;
        set
        {
            if (SetProperty(ref _busy, value))
            {
                OnPropertyChanged(nameof(CountDisplay));
            }
        }
    }

    public string CountDisplay => Busy ? "Deleting…" : $"{_count:N0} {(_count == 1 ? "row" : "rows")}";
}

/// <summary>An account's deletable data: the per-type items plus a "delete everything" command.</summary>
public sealed class AccountStoredData
{
    public AccountStoredData(string accountId, string accountName)
    {
        AccountId = accountId;
        AccountName = accountName;
    }

    public string AccountId { get; }

    public string AccountName { get; }

    public ObservableCollection<StoredDataItem> Types { get; } = new();

    public RelayCommand DeleteAll { get; set; } = null!;
}
