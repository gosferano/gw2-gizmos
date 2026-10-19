using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A filter choice on the Achievements page.</summary>
public sealed record AchievementFilterOption(AchievementFilter Filter, string Label);

/// <summary>A sort choice on the Achievements page.</summary>
public sealed record AchievementSortOption(AchievementSort Sort, string Label);

/// <summary>
/// Backs the Achievements page: the game's groups and categories as a scope tree (with all achievements on top, so the
/// list can span every category), and the achievements in the chosen scope, filtered by progress and name, in the
/// chosen order. Reloads after account syncs.
/// </summary>
public sealed class AchievementsViewModel : ViewModelBase, INavigationAware
{
    private readonly AchievementsReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private AchievementsCatalog _catalog = AchievementsCatalog.Empty;
    private AchievementScope? _selectedScope;
    private AchievementFilterOption _filter;
    private AchievementSortOption _sort;
    private string _search = "";
    private string _summary = "";
    private IReadOnlyList<AchievementRow> _rows = [];

    // Which achievements are open, by id, so a reload (after a sync) keeps them open.
    private readonly HashSet<int> _expanded = [];
    private bool _isLoaded;
    private bool _showHidden;
    private AchievementsData _data = AchievementsData.Empty;

    public AchievementsViewModel(
        AchievementsReader reader,
        SelectedAccountService account,
        DataChangeMonitor dataChanges
    )
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(account.AccountId) ? null : account.AccountId;
        _filter = Filters[0];
        _sort = Sorts[0];

        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public IReadOnlyList<AchievementFilterOption> Filters { get; } =
    [
        new(AchievementFilter.All, "All"),
        new(AchievementFilter.Unfinished, "Unfinished"),
        new(AchievementFilter.InProgress, "In progress"),
        new(AchievementFilter.NotStarted, "Not started"),
        new(AchievementFilter.Finished, "Finished"),
    ];

    public IReadOnlyList<AchievementSortOption> Sorts { get; } =
    [
        new(AchievementSort.GameOrder, "Game order"),
        new(AchievementSort.ClosestToNextTier, "Closest to next tier"),
        new(AchievementSort.TotalCompletion, "Total completion"),
        new(AchievementSort.PointsLeft, "Most points left"),
        new(AchievementSort.Name, "Name"),
    ];

    /// <summary>The scope tree's top: "All achievements", whose children are the groups.</summary>
    public ObservableCollection<AchievementScope> Scopes { get; } = new();

    /// <summary>
    /// The achievements the list shows. Swapped whole on each change, so the list resets once: matching thousands of
    /// rows one by one (as small lists do) blocked the UI and scrolled the list as each change landed.
    /// </summary>
    public IReadOnlyList<AchievementRow> Rows
    {
        get => _rows;
        private set => SetProperty(ref _rows, value);
    }

    public AchievementScope? SelectedScope
    {
        get => _selectedScope;
        set
        {
            if (SetProperty(ref _selectedScope, value))
            {
                Apply();
            }
        }
    }

    public AchievementFilterOption Filter
    {
        get => _filter;
        set
        {
            if (value is not null && SetProperty(ref _filter, value))
            {
                Apply();
            }
        }
    }

    public AchievementSortOption Sort
    {
        get => _sort;
        set
        {
            if (value is not null && SetProperty(ref _sort, value))
            {
                Apply();
            }
        }
    }

    /// <summary>Also list achievements the game hides until they're started; off by default.</summary>
    public bool ShowHidden
    {
        get => _showHidden;
        set
        {
            if (SetProperty(ref _showHidden, value))
            {
                _ = RebuildAsync();
            }
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value ?? ""))
            {
                Apply();
            }
        }
    }

    /// <summary>False when no account is selected: the achievements still show, without progress.</summary>
    public bool HasAccount => _accountId is not null;

    /// <summary>True once achievements have synced; until then the page shows a note.</summary>
    public bool HasAchievements => _catalog.Rows.Count > 0;

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetProperty(ref _isLoaded, value);
    }

    /// <summary>E.g. "1,234 achievements · 5,678 / 9,000 AP" for what the list shows.</summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private async Task LoadAsync()
    {
        try
        {
            _data = await Task.Run(() => _reader.Read(_accountId));
        }
        catch (Exception)
        {
            return; // Keep what's shown on a read failure.
        }

        await RebuildAsync();
    }

    /// <summary>Rebuilds the list and scope tree from the loaded data, e.g. when hidden ones are toggled.</summary>
    private async Task RebuildAsync()
    {
        AchievementsData data = _data;
        bool showHidden = _showHidden;
        AchievementsCatalog catalog = await Task.Run(() => AchievementsCatalog.Build(data, showHidden));

        // Merge into the tree shown rather than replacing it, so expanded nodes and the selection stay as they were.
        if (Scopes.Count == 1 && Scopes[0].Name == catalog.Root.Name)
        {
            Scopes[0].MergeFrom(catalog.Root);
            _catalog = catalog.WithRoot(Scopes[0]);
        }
        else
        {
            _catalog = catalog;
            Scopes.ReplaceWith([catalog.Root]);
        }

        // The chosen scope may have gone (a category holding only hidden achievements, once they're hidden).
        if (_selectedScope is null || !_catalog.Root.Contains(_selectedScope))
        {
            _selectedScope = _catalog.Root;
            OnPropertyChanged(nameof(SelectedScope));
        }

        OnPropertyChanged(nameof(HasAchievements));
        IsLoaded = true;
        Apply();
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is AchievementRow row && e.PropertyName == nameof(AchievementRow.IsExpanded))
        {
            if (row.IsExpanded)
            {
                _expanded.Add(row.Id);
            }
            else
            {
                _expanded.Remove(row.Id);
            }
        }
    }

    private void Apply()
    {
        AchievementScope scope = _selectedScope ?? _catalog.Root;
        IReadOnlyList<AchievementRow> rows = _catalog.Select(scope, _filter.Filter, _sort.Sort, _search);
        foreach (AchievementRow row in rows)
        {
            row.IsExpanded = _expanded.Contains(row.Id);
            row.PropertyChanged -= OnRowChanged;
            row.PropertyChanged += OnRowChanged;
        }

        Rows = rows;

        int earned = rows.Sum(row => row.PointsEarned);
        int total = rows.Sum(row => row.PointsTotal);
        string count = rows.Count.ToString("N0", CultureInfo.CurrentCulture);
        Summary = HasAccount
            ? $"{count} achievements · {earned.ToString("N0", CultureInfo.CurrentCulture)} / "
                + $"{total.ToString("N0", CultureInfo.CurrentCulture)} AP"
            : $"{count} achievements";
    }
}
