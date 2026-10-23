using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A kind's heading in the cleanup list, with what the suggestions under it mean, what they're worth selling
/// instantly, and what listing the ones where it pays would bring instead (null when it doesn't anywhere).
/// </summary>
public sealed record CleanupHeading(string Title, string Description, long Copper, long? ListCopper = null)
{
    public bool HasCopper => Copper > 0;

    /// <summary>E.g. "or 12g 40s listing where it pays".</summary>
    public string ListText => ListCopper is { } listed ? $"or {Coin.Format(listed)} listing where it pays" : "";

    public bool HasList => ListCopper is not null;
}

/// <summary>A material storage capacity to pick: a size, or the guess from what's stored (null).</summary>
public sealed record CapacityOption(string Label, int? Capacity);

/// <summary>A suggestion's row on the Inventory cleanup page.</summary>
public sealed class CleanupRow
{
    private readonly CleanupSuggestion _suggestion;
    private readonly int _capacity;

    public CleanupRow(
        CleanupSuggestion suggestion, string name, int capacity, bool isKept, RelayCommand<CleanupRow> keepCommand)
    {
        _suggestion = suggestion;
        _capacity = capacity;
        Name = name;
        IsKept = isKept;
        KeepCommand = keepCommand;
    }

    public int ItemId => _suggestion.ItemId;

    public string Name { get; }

    /// <summary>Why it's suggested, e.g. "299 in 3 slots fit in 2".</summary>
    public string Reason => _suggestion.Kind switch
    {
        CleanupKind.Stack when _suggestion.Slots is var (now, after) =>
            $"{Number(_suggestion.Count)} in {now} slots fit in {after}",
        CleanupKind.Deposit => $"Material storage has room for {Number(_suggestion.Count)}",
        CleanupKind.Overflow =>
            $"{Number(_suggestion.Count)} more than material storage holds ({Number(_capacity)} each)",
        CleanupKind.Open => $"{Number(_suggestion.Count)} to open",
        CleanupKind.Sell when _suggestion.Junk => $"{Number(_suggestion.Count)} junk, only good for a vendor",
        CleanupKind.Sell => $"{Number(_suggestion.Count)} that no recipe or unfinished achievement uses",
        CleanupKind.Consume => $"{Number(_suggestion.Count)}, for a {_suggestion.Unlocks} you don't have yet",
        CleanupKind.Unlocked => $"{Number(_suggestion.Count)}, for a {_suggestion.Unlocks} you already have",
        CleanupKind.Leftover => $"{Number(_suggestion.Count)}, only for {DoneForText}, which you've done",
        _ => "",
    };

    // E.g. "Bloodstone Fen Insight" or "Lost Bandit Chest and 2 more".
    private string DoneForText => _suggestion.DoneFor.Count switch
    {
        1 => _suggestion.DoneFor[0],
        2 => $"{_suggestion.DoneFor[0]} and {_suggestion.DoneFor[1]}",
        _ => $"{_suggestion.DoneFor[0]} and {_suggestion.DoneFor.Count - 1} more",
    };

    /// <summary>E.g. "List them for 2g 10s after fees instead; about 40 sell a week".</summary>
    public string ListText => _suggestion.ListCopper is { } listed
        ? $"List {(_suggestion.Count == 1 ? "it" : "them")} for {Coin.Format(listed)} after fees instead; about "
            + $"{Number(_suggestion.SoldPerWeek ?? 0)} sell a week"
        : "";

    public bool HasList => _suggestion.ListCopper is not null;

    /// <summary>E.g. "In the bank (115), Gosferano (65)".</summary>
    public string PlacesText => "In " + string.Join(", ", _suggestion.Places.Select(p =>
        $"{(p.Place == "Bank" ? "the bank" : p.Place == "Shared inventory" ? "shared inventory" : p.Place)}"
        + $" ({Number(p.Count)})"));

    public long Copper => _suggestion.Copper;

    public bool HasCopper => Copper > 0;

    public bool IsKept { get; }

    public string KeepLabel => IsKept ? "Unkeep" : "Keep";

    public string KeepTip => IsKept
        ? "Suggest things for this item again"
        : "Keep this item: stop suggesting anything for it";

    public RelayCommand<CleanupRow> KeepCommand { get; }

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}

/// <summary>
/// Backs the Advisor › Inventory cleanup page: what to stack, deposit, open or sell across the bank, shared inventory
/// and bags (<see cref="InventoryCleanup"/>), grouped by kind, each with its reason. An item can be kept, hiding its
/// suggestions. Refreshes after syncs and price polls.
/// </summary>
public sealed class InventoryCleanupViewModel : ViewModelBase, INavigationAware
{
    private static readonly (CleanupKind Kind, string Title, string Description)[] Kinds =
    [
        (CleanupKind.Stack, "Stack", "The same item in partial stacks, in fewer slots."),
        (CleanupKind.Deposit, "Deposit", "Materials that material storage has room for."),
        (CleanupKind.Overflow, "Overflow", "Materials beyond what material storage holds, taking up slots."),
        (CleanupKind.Open, "Open", "Containers waiting to be opened."),
        (CleanupKind.Consume, "Use", "Recipe sheets, dyes, minis and skins you haven't unlocked yet."),
        (CleanupKind.Unlocked, "Already unlocked", "Recipe sheets, dyes, minis and skins you already have: sell or "
            + "salvage them."),
        (CleanupKind.Leftover, "Leftovers", "Items with no use of their own that only finished achievements and "
            + "collections asked for. Sell what's worth something; the rest can go."),
        (CleanupKind.Sell, "Sell", "Items nothing in your account uses: no recipe takes them, and no unfinished "
            + "achievement asks for them, and junk. Other bound copies are left out, as are food, tomes and gizmos: "
            + "they're for using."),
    ];

    private readonly AccountReader _reader;
    private readonly InventoryCleanupSettingsStore _settings;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly RelayCommand<CleanupRow> _keepCommand;
    private CleanupData _data = CleanupData.Empty;
    private bool _showKept;
    private bool _loaded;

    public InventoryCleanupViewModel(
        AccountReader reader,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges,
        InventoryCleanupSettingsStore settings
    )
    {
        _reader = reader;
        _settings = settings;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = AdvisorBreadcrumbs.Section("Inventory cleanup");
        _keepCommand = new RelayCommand<CleanupRow>(row =>
        {
            if (row is not null)
            {
                _settings.Keep(row.ItemId, !row.IsKept);
                Apply();
            }
        });
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

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    /// <summary>The material storage capacities to pick from, the guess first.</summary>
    public IReadOnlyList<CapacityOption> Capacities { get; private set; } = [];

    /// <summary>How much material storage holds per item.</summary>
    public CapacityOption? Capacity
    {
        get => Capacities.FirstOrDefault(c => c.Capacity == _settings.StorageCapacity);
        set
        {
            if (value is not null && value.Capacity != _settings.StorageCapacity)
            {
                _settings.StorageCapacity = value.Capacity;
                OnPropertyChanged();
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Also list the kept items' suggestions, to unkeep them.</summary>
    public bool ShowKept
    {
        get => _showKept;
        set
        {
            if (SetProperty(ref _showKept, value))
            {
                Apply();
            }
        }
    }

    /// <summary>How many items are kept, e.g. "Show 3 kept items".</summary>
    public string ShowKeptLabel => $"Show kept items ({_settings.Kept.Count})";

    /// <summary>
    /// One list, so it can be virtualized: each kind's suggestions under a <see cref="CleanupHeading"/>, those worth
    /// the most first.
    /// </summary>
    public IReadOnlyList<object> Rows { get; private set; } = [];

    public bool NoSuggestions => _loaded && Rows.Count == 0;

    /// <summary>The suggestions to show: all but the kept items', unless those are asked for.</summary>
    public static IEnumerable<CleanupSuggestion> Shown(CleanupData data, IReadOnlySet<int> kept, bool showKept) =>
        data.Suggestions.Where(s => showKept || !kept.Contains(s.ItemId));

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            int? capacity = _settings.StorageCapacity;
            _data = await Task.Run(() => _reader.GetCleanup(accountId, capacity));
            _loaded = true;
            Capacities = InventoryCleanupSettingsStore.CapacityChoices
                .Select(c => new CapacityOption($"{c:N0} per item", c))
                .Prepend(new CapacityOption($"Guess ({_data.GuessedCapacity:N0} per item)", null))
                .ToList();
            OnPropertyChanged(nameof(Capacities));
            OnPropertyChanged(nameof(Capacity));
            Apply();
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void Apply()
    {
        IReadOnlySet<int> kept = _settings.Kept;
        int capacity = _settings.StorageCapacity ?? _data.GuessedCapacity;
        List<CleanupSuggestion> shown = Shown(_data, kept, _showKept).ToList();
        var rows = new List<object>();
        foreach ((CleanupKind kind, string title, string description) in Kinds)
        {
            List<CleanupSuggestion> ofKind = shown.Where(s => s.Kind == kind)
                .OrderByDescending(s => s.Copper)
                .ThenByDescending(s => s.Slots?.Now - s.Slots?.After ?? 0)
                .ThenByDescending(s => s.Count)
                .ToList();
            if (ofKind.Count == 0)
            {
                continue;
            }

            rows.Add(new CleanupHeading(title, description, ofKind.Sum(s => s.Copper),
                ofKind.Any(s => s.ListCopper is not null) ? ofKind.Sum(s => s.ListCopper ?? s.Copper) : null));
            rows.AddRange(ofKind.Select(s => new CleanupRow(
                s, _data.Names.GetValueOrDefault(s.ItemId) ?? $"Item {s.ItemId}", capacity, kept.Contains(s.ItemId),
                _keepCommand)));
        }

        Rows = rows;
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(NoSuggestions));
        OnPropertyChanged(nameof(ShowKeptLabel));
    }
}
