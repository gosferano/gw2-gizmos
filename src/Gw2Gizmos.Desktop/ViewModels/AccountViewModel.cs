using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Account page — the per-account sections hub: the current account's name + last-synced time in the
/// header, over the Wallet / Material storage / Bank / Shared inventory cards. The account is the global
/// selection (<see cref="SelectedAccountService"/>), picked on the API Keys page; this page just reflects it.
/// </summary>
public sealed class AccountViewModel : ViewModelBase
{
    private string _summary = "";
    private string? _summaryTooltip;

    public AccountViewModel(SelectedAccountService selected, AccountReader reader)
    {
        AccountName = selected.AccountName ?? "";
        HasAccount = !string.IsNullOrEmpty(selected.AccountId);

        if (selected.AccountId is { } accountId)
        {
            _ = LoadSyncTimeAsync(reader, accountId);
        }
    }

    public string AccountName { get; }

    /// <summary>False when no account is selected/added yet; the page shows a "pick an account" note.</summary>
    public bool HasAccount { get; }

    /// <summary>When this account last synced, relative (e.g. "Synced 3m ago").</summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    /// <summary>Exact last-sync timestamp, shown as the tooltip beside <see cref="Summary"/>; null until loaded.</summary>
    public string? SummaryTooltip
    {
        get => _summaryTooltip;
        private set => SetProperty(ref _summaryTooltip, value);
    }

    private async Task LoadSyncTimeAsync(AccountReader reader, string accountId)
    {
        // Off the UI thread; the synced Account row may not exist yet on a freshly-added key.
        AccountInfo? account = await Task.Run(() => reader.GetAccounts().FirstOrDefault(a => a.Id == accountId));
        if (account is null)
        {
            Summary = "Waiting for first sync…";
            return;
        }

        Summary = $"Synced {RelativeTime.Format(account.LastSyncedUtc)}";
        SummaryTooltip = RelativeTime.Exact(account.LastSyncedUtc);
    }
}

/// <summary>A wallet currency and its current amount, with its icon URL for display.</summary>
public sealed record WalletRow(string Name, long Value, string IconUrl);

/// <summary>A material-storage category and its item cells (in-game order), for the grouped grid.</summary>
public sealed record MaterialCategoryView(string Name, System.Collections.Generic.IReadOnlyList<SlotRow> Items);

/// <summary>
/// One cell in a slot/material grid: an item icon + count. <see cref="ItemId"/> 0 is an empty bank slot
/// (renders a placeholder). For material cells the count is always shown; for bank/inventory only stacks of &gt;1.
/// </summary>
public sealed record SlotRow(int ItemId, string Name, int Count)
{
    /// <summary>An empty bank/inventory slot (renders a placeholder tile, not an icon).</summary>
    public bool IsEmpty => ItemId <= 0;

    /// <summary>Show the corner count badge for bank/inventory stacks of more than one.</summary>
    public bool HasCount => Count > 1;

    /// <summary>A material the account doesn't currently hold (count 0); its cell renders greyscale.</summary>
    public bool IsZero => Count <= 0;

    /// <summary>Count text (always rendered for material cells).</summary>
    public string CountDisplay => Count.ToString("N0");

    public static SlotRow Empty { get; } = new(0, "", 0);
}
