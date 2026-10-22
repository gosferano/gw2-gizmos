using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Advisor page: a hub of advice drawn from the account's own data, each section with a summary of what it
/// found, opening its own page. Refreshes after syncs and price polls.
/// </summary>
public sealed class AdvisorViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly CraftFromStockSettingsStore _craftSettings;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private string _craftSummary = "";
    private long _bestCraftCopper;
    private bool _hasBestCraft;

    public AdvisorViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges,
        INavigationService navigation,
        CraftFromStockSettingsStore craftSettings
    )
    {
        _reader = reader;
        _craftSettings = craftSettings;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        OpenCommand = new RelayCommand<AppPage>(page => navigation.NavigateTo(page));
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(changes =>
        {
            if ((changes & (DataChanges.AccountSync | DataChanges.Holdings | DataChanges.Catalog)) != 0)
            {
                _ = LoadAsync();
            }
        });
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    /// <summary>Opens a section's page.</summary>
    public RelayCommand<AppPage> OpenCommand { get; }

    /// <summary>E.g. "42 crafts worth more than their materials · best: Xunlai Electrum Ingot".</summary>
    public string CraftSummary
    {
        get => _craftSummary;
        private set => SetProperty(ref _craftSummary, value);
    }

    /// <summary>What the best craft made entirely from stock makes, crafting all it can.</summary>
    public long BestCraftCopper
    {
        get => _bestCraftCopper;
        private set => SetProperty(ref _bestCraftCopper, value);
    }

    public bool HasBestCraft
    {
        get => _hasBestCraft;
        private set => SetProperty(ref _hasBestCraft, value);
    }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            CraftSummary = "Needs an account: add an API key on the API keys page";
            return;
        }

        try
        {
            StockCrafts crafts = await Task.Run(() => _reader.GetStockCrafts(accountId));
            // With the page's filters.
            List<StockCraft> shown = CraftFromStockViewModel
                .Shown(crafts, _craftSettings.OnlyCraftable, _craftSettings.Minimum.Copper)
                .ToList();
            StockCraft? best = shown.Where(c => c.Missing.Count == 0).MaxBy(c => c.StockProfitCopper);
            int ready = shown.Count(c => c.Missing.Count == 0);
            CraftSummary = shown.Count == 0
                ? "Nothing you hold is worth more crafted than sold, with the page's filters"
                : string.Join(" · ", new[]
                {
                    $"{Number(shown.Count)} {(shown.Count == 1 ? "craft" : "crafts")} worth more than their materials",
                    ready < shown.Count ? $"{Number(ready)} without buying anything" : null,
                    best is not null ? $"best: {crafts.Names.GetValueOrDefault(best.Recipe.OutputItemId)}" : null,
                }.Where(part => part is not null));
            BestCraftCopper = best?.StockProfitCopper ?? 0;
            HasBestCraft = best is not null;
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
