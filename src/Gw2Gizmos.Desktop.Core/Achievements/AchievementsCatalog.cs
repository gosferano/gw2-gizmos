using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Which achievements the list shows by the account's progress.</summary>
public enum AchievementFilter
{
    All,
    Unfinished,
    InProgress,
    NotStarted,
    Finished,
}

/// <summary>How the list is ordered.</summary>
public enum AchievementSort
{
    GameOrder,
    ClosestToNextTier,
    TotalCompletion,
    PointsLeft,
    Name,
}

/// <summary>
/// A node of the Achievements page's scope tree: all achievements, a group (e.g. "Fractals") or one of its
/// categories, with how many of its achievements are done.
/// </summary>
public sealed class AchievementScope : ViewModelBase
{
    private IReadOnlySet<int> _achievementIds;
    private int _doneCount;
    private int _totalCount;
    private bool _isExpanded;
    private bool _isRoot;

    public AchievementScope(
        string name,
        IReadOnlyList<AchievementScope> children,
        IReadOnlyCollection<AchievementRow> rows,
        string? icon = null
    )
    {
        Name = name;
        Icon = icon;
        Children = new ObservableCollection<AchievementScope>(children);
        _achievementIds = rows.Select(row => row.Id).ToHashSet();
        _doneCount = rows.Count(row => row.IsDone);
        _totalCount = rows.Count;
    }

    public string Name { get; }

    /// <summary>The category's icon; null for groups and the top, which the API gives none.</summary>
    public string? Icon { get; }

    public bool HasIcon => !string.IsNullOrEmpty(Icon);
    public ObservableCollection<AchievementScope> Children { get; }

    /// <summary>The tree's top ("All achievements"), which starts expanded.</summary>
    public bool IsRoot
    {
        get => _isRoot;
        init
        {
            _isRoot = value;
            _isExpanded = value;
        }
    }

    /// <summary>Whether the node is open in the tree; kept here so a rebuild doesn't collapse it.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public IReadOnlySet<int> AchievementIds => _achievementIds;
    public int DoneCount => _doneCount;
    public int TotalCount => _totalCount;

    /// <summary>E.g. "37 / 52".</summary>
    public string CountText => $"{Number(DoneCount)} / {Number(TotalCount)}";

    /// <summary>
    /// Brings this node in line with a freshly built one of the same name: its counts updated in place, and its
    /// children matched by name, kept (and merged in turn) when still there, added or removed otherwise. The tree then
    /// keeps its items, so expanded nodes and the selection stay as they were.
    /// </summary>
    public void MergeFrom(AchievementScope fresh)
    {
        _achievementIds = fresh._achievementIds;
        _doneCount = fresh._doneCount;
        _totalCount = fresh._totalCount;
        OnPropertyChanged(nameof(AchievementIds));
        OnPropertyChanged(nameof(DoneCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(CountText));

        var merged = new List<AchievementScope>();
        foreach (AchievementScope child in fresh.Children)
        {
            AchievementScope? existing = Children.FirstOrDefault(c => c.Name == child.Name && !merged.Contains(c));
            if (existing is null)
            {
                merged.Add(child);
            }
            else
            {
                existing.MergeFrom(child);
                merged.Add(existing);
            }
        }

        Children.ReplaceWith(merged);
    }

    /// <summary>Whether <paramref name="scope"/> is this node or one below it.</summary>
    public bool Contains(AchievementScope scope) =>
        ReferenceEquals(this, scope) || Children.Any(child => child.Contains(scope));

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}

/// <summary>
/// The achievements as the page lists them: one row each in the game's order, the scope tree of groups and
/// categories, and the filtering and sorting of the list. Achievements the game keeps hidden until they're started
/// are left out unless asked for. The rotating categories (dailies, weeklies) list only what's active now, as the API
/// does.
/// </summary>
public sealed class AchievementsCatalog
{
    private AchievementsCatalog(IReadOnlyList<AchievementRow> rows, AchievementScope root)
    {
        Rows = rows;
        Root = root;
    }

    public static AchievementsCatalog Empty { get; } =
        new([], new AchievementScope("All achievements", [], []) { IsRoot = true });

    /// <summary>Every listed achievement once, in the game's order.</summary>
    public IReadOnlyList<AchievementRow> Rows { get; }

    /// <summary>"All achievements", with the groups and their categories below it.</summary>
    public AchievementScope Root { get; }

    /// <summary>The same achievements with another (merged) scope tree.</summary>
    public AchievementsCatalog WithRoot(AchievementScope root) => new(Rows, root);

    /// <param name="includeHidden">Also list achievements the game hides until they're started.</param>
    public static AchievementsCatalog Build(AchievementsData data, bool includeHidden = false)
    {
        var rows = new Dictionary<int, AchievementRow>();
        var groupScopes = new List<AchievementScope>();
        foreach (AchievementGroup group in data.Groups.OrderBy(g => g.Order))
        {
            var categoryScopes = new List<AchievementScope>();
            IEnumerable<AchievementCategory> categories = group
                .CategoryIds.Select(id => data.Categories.GetValueOrDefault(id))
                .OfType<AchievementCategory>()
                .OrderBy(c => c.Order);
            var groupRows = new List<AchievementRow>();
            foreach (AchievementCategory category in categories)
            {
                var categoryRows = new List<AchievementRow>();
                foreach (int id in category.AchievementIds)
                {
                    if (!data.Achievements.TryGetValue(id, out Achievement? achievement))
                    {
                        continue;
                    }

                    AccountAchievement? progress = data.Progress.GetValueOrDefault(id);
                    if (!includeHidden && IsHidden(achievement, progress))
                    {
                        continue;
                    }

                    // An achievement in several categories is one row, placed where the game first lists it.
                    if (!rows.TryGetValue(id, out AchievementRow? row))
                    {
                        row = new AchievementRow(achievement, progress, category, group.Name, rows.Count, data);
                        rows[id] = row;
                    }

                    categoryRows.Add(row);
                }

                if (categoryRows.Count > 0)
                {
                    categoryScopes.Add(new AchievementScope(category.Name, [], categoryRows, category.Icon));
                    groupRows.AddRange(categoryRows);
                }
            }

            if (categoryScopes.Count > 0)
            {
                List<AchievementRow> distinct = groupRows.DistinctBy(r => r.Id).ToList();
                groupScopes.Add(new AchievementScope(group.Name, categoryScopes, distinct));
            }
        }

        List<AchievementRow> all = rows.Values.OrderBy(row => row.GameOrder).ToList();
        return new AchievementsCatalog(
            all,
            new AchievementScope("All achievements", groupScopes, all) { IsRoot = true }
        );
    }

    /// <summary>The rows in <paramref name="scope"/> that pass the filter and search, in the chosen order.</summary>
    public IReadOnlyList<AchievementRow> Select(
        AchievementScope scope,
        AchievementFilter filter,
        AchievementSort sort,
        string search
    )
    {
        string term = search.Trim();
        IEnumerable<AchievementRow> rows = Rows.Where(row =>
            scope.AchievementIds.Contains(row.Id)
            && Passes(row, filter)
            && (term.Length == 0 || row.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
        );

        return sort switch
        {
            // Quick wins first: the nearest to their next tier, then the fewest steps left; the rest after, in game
            // order.
            AchievementSort.ClosestToNextTier => rows
                .OrderByDescending(row => row.IsQuickWinCandidate)
                .ThenByDescending(row => row.IsQuickWinCandidate ? row.NextTierFraction : 0)
                .ThenBy(row => row.IsQuickWinCandidate ? row.ToNextTier : 0)
                .ThenBy(row => row.GameOrder)
                .ToList(),
            // The furthest along first; finished ones last.
            AchievementSort.TotalCompletion => rows
                .OrderBy(row => row.IsDone)
                .ThenByDescending(row => row.TotalFraction)
                .ThenBy(row => row.GameOrder)
                .ToList(),
            AchievementSort.PointsLeft => rows
                .OrderByDescending(row => row.PointsLeft)
                .ThenBy(row => row.GameOrder)
                .ToList(),
            AchievementSort.Name => rows.OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            _ => rows.OrderBy(row => row.GameOrder).ToList(),
        };
    }

    private static bool Passes(AchievementRow row, AchievementFilter filter) =>
        filter switch
        {
            AchievementFilter.Unfinished => !row.IsDone,
            AchievementFilter.InProgress => row.Status == AchievementStatus.InProgress,
            AchievementFilter.NotStarted => row.Status == AchievementStatus.NotStarted,
            AchievementFilter.Finished => row.IsDone,
            _ => true,
        };

    /// <summary>Whether the game still hides it: flagged hidden and not started.</summary>
    internal static bool IsHidden(Achievement achievement, AccountAchievement? progress) =>
        achievement.Flags.Contains("Hidden") && progress is null;
}
