using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Account landing page: the current account header (name/world/last sync) and — via the page's
/// cards — links to the per-section sub-pages. Cheap: just one identity query, resolved synchronously so the
/// empty-state decision doesn't flicker. Each sub-page loads its own section fresh on navigation.
/// </summary>
public sealed class AccountViewModel : ViewModelBase
{
    private bool _hasData;
    private string _accountName = "";
    private string _summary = "";

    public AccountViewModel(AccountReader reader) => _ = LoadAsync(reader);

    /// <summary>True once an account has been synced; the page shows a "waiting for sync" note otherwise.</summary>
    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public string AccountName
    {
        get => _accountName;
        private set => SetProperty(ref _accountName, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private async Task LoadAsync(AccountReader reader)
    {
        // Off the UI thread so navigation doesn't block (the first DB touch pays EF's cold-start cost too).
        AccountInfo? account = await Task.Run(reader.GetCurrentAccount);
        if (account is null)
        {
            return;
        }

        AccountName = account.Name;
        Summary = $"World {account.World} · synced {account.LastSyncedUtc.LocalDateTime:g}";
        HasData = true; // set last so the page flips to the loaded view in a single step
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
