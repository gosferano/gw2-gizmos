using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A once-per-reset activity on the Daily and weekly page: dimmed when done, and when not, how long until its event
/// next starts (or how long it has left), ticking.
/// </summary>
public sealed class ResetActivityRow : ViewModelBase
{
    private readonly ResetActivity _activity;
    private string _name;
    private string? _chatLink;
    private string _countdown = "";
    private bool _isActive;
    private bool _isSoon;

    public ResetActivityRow(ResetActivity activity, DateTimeOffset now)
    {
        _activity = activity;
        _name = activity.Name;
        ItemId = activity.ItemId;
        IsDone = activity.Done;
        if (!activity.Done && activity.AnyTime)
        {
            _countdown = "any time";
        }

        Tick(now);
    }

    /// <summary>Its name; for a chest shared by several metas (the Convergences), the one that's on or next.</summary>
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    /// <summary>The item it is (a daily craft), for its icon and link; 0 for the other kinds.</summary>
    public int ItemId { get; }

    public bool HasItem => ItemId > 0;

    public bool IsDone { get; }

    /// <summary>
    /// The chat link to its event's waypoint (the one on or next), to paste in game; null when not on the Events
    /// page's schedule.
    /// </summary>
    public string? ChatLink
    {
        get => _chatLink;
        private set
        {
            if (SetProperty(ref _chatLink, value))
            {
                OnPropertyChanged(nameof(HasChatLink));
            }
        }
    }

    public bool HasChatLink => !string.IsNullOrEmpty(ChatLink);

    /// <summary>
    /// E.g. "in 12:34", "active · 8:12 left", or "any time" for a meta not on a timer; empty when done or not known.
    /// </summary>
    public string Countdown
    {
        get => _countdown;
        private set => SetProperty(ref _countdown, value);
    }

    /// <summary>Its event is on now (the countdown reads green).</summary>
    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    /// <summary>Its event starts within <see cref="EventTiming.SoonWindow"/> (the countdown reads yellow).</summary>
    public bool IsSoon
    {
        get => _isSoon;
        private set => SetProperty(ref _isSoon, value);
    }

    /// <summary>When its event next ends (if active) or starts, for ordering; null without a scheduled event.</summary>
    public DateTimeOffset? SortKey { get; private set; }

    public void Tick(DateTimeOffset now)
    {
        if (_activity.Next(now) is not var (next, timing))
        {
            return;
        }

        ChatLink = next.ChatLink;
        if (_activity.Events.Count > 1)
        {
            Name = next.Name;
        }

        if (IsDone)
        {
            return;
        }

        SortKey = timing.SortKey;
        Countdown = timing.Countdown;
        IsActive = timing.IsActive;
        IsSoon = timing.IsSoon;
    }
}

/// <summary>
/// A card of once-per-reset activities (world bosses, map chests, daily crafting): what's left first, soonest
/// first, then what's done. Long lists show their first few until expanded.
/// </summary>
public sealed class ResetActivitySection : ViewModelBase
{
    private const int CollapsedCount = 6;

    private IReadOnlyList<ResetActivityRow> _all = [];
    private bool _showAll;
    private string _summary = "";

    public ResetActivitySection(string title, string when)
    {
        Title = title;
        When = when;
        ToggleCommand = new RelayCommand(() =>
        {
            _showAll = !_showAll;
            Changed();
        });
    }

    public string Title { get; }

    /// <summary>"today" or "this week", for the summary.</summary>
    private string When { get; }

    /// <summary>E.g. "4 of 15 today".</summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public IReadOnlyList<ResetActivityRow> Rows => _showAll ? _all : _all.Take(CollapsedCount).ToList();

    public bool HasMore => _all.Count > CollapsedCount;

    public string ToggleText => _showAll ? "Show fewer" : $"Show all {_all.Count}";

    public RelayCommand ToggleCommand { get; }

    public void Set(IReadOnlyList<ResetActivity> activities, DateTimeOffset now)
    {
        List<ResetActivityRow> rows = activities.Select(a => new ResetActivityRow(a, now)).ToList();
        // What's left before what's done; within what's left, the soonest event first, then the unscheduled ones —
        // each group keeps the API's order otherwise (OrderBy is stable).
        _all = rows
            .OrderBy(r => r.IsDone)
            .ThenBy(r => r.SortKey ?? DateTimeOffset.MaxValue)
            .ToList();
        Summary = $"{rows.Count(r => r.IsDone)} of {rows.Count} {When}";
        Changed();
    }

    public void Tick(DateTimeOffset now)
    {
        foreach (ResetActivityRow row in _all)
        {
            row.Tick(now);
        }
    }

    private void Changed()
    {
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(ToggleText));
    }
}

/// <summary>
/// A raid wing or a dungeon on the page: how many of its bosses or paths are done, each listed in its tooltip.
/// </summary>
public sealed class ResetActivityGroupRow
{
    public ResetActivityGroupRow(ResetActivityGroup group)
    {
        Name = group.Name;
        IsDone = group.Done;
        Summary = $"{group.Cleared} of {group.Activities.Count}";
        Tooltip = string.Join("\n", group.Activities.Select(a => $"{(a.Done ? "✓" : "○")} {a.Name}"));
    }

    public string Name { get; }

    public bool IsDone { get; }

    /// <summary>E.g. "3 of 4".</summary>
    public string Summary { get; }

    public string Tooltip { get; }
}

/// <summary>A Wizard's Vault objective on the page.</summary>
public sealed class WizardsVaultObjectiveRow
{
    public WizardsVaultObjectiveRow(WizardsVaultObjectiveLine line)
    {
        Title = line.Title;
        Track = line.Track;
        IsClaimed = line.Claimed;
        IsReadyToClaim = line.ReadyToClaim;
        AcclaimText = $"{line.Acclaim} AA";
        // Some objectives come without a target (0); a "0 / 0" would say nothing.
        ProgressText = line.Complete > 0
            ? $"{Number(Math.Min(line.Current, line.Complete))} / {Number(line.Complete)}"
            : "";
        Fraction = line.Complete > 0 ? Math.Clamp((double)line.Current / line.Complete, 0, 1) : 0;
    }

    public string Title { get; }

    /// <summary>"PvE", "PvP" or "WvW".</summary>
    public string Track { get; }

    public bool IsClaimed { get; }

    /// <summary>Done, its Astral Acclaim waiting to be claimed in game.</summary>
    public bool IsReadyToClaim { get; }

    public bool IsInProgress => !IsClaimed && !IsReadyToClaim;

    /// <summary>E.g. "10 AA".</summary>
    public string AcclaimText { get; }

    /// <summary>E.g. "11 / 20".</summary>
    public string ProgressText { get; }

    public double Fraction { get; }

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}

/// <summary>One of the Wizard's Vault's sets (daily, weekly, special) on the page.</summary>
public sealed class WizardsVaultGroup
{
    public WizardsVaultGroup(WizardsVaultSet set)
    {
        Title = set.Period;
        IsStale = set.IsStale;
        Objectives = set.IsStale ? [] : set.Objectives.Select(o => new WizardsVaultObjectiveRow(o)).ToList();
        string done = $"{set.Done} of {set.Objectives.Count} done";
        Summary =
            set.IsStale ? "Log in to the game to get the new objectives"
            : set.Meta is { } meta
                ? $"{done} · meta reward {meta.RewardAstral} AA{(meta.RewardClaimed ? ", claimed" : "")}"
            : done;
    }

    public string Title { get; }

    /// <summary>Synced before the reset that replaced its objectives.</summary>
    public bool IsStale { get; }

    /// <summary>E.g. "3 of 4 done · meta reward 20 AA".</summary>
    public string Summary { get; }

    public IReadOnlyList<WizardsVaultObjectiveRow> Objectives { get; }
}

/// <summary>An achievement of a rotating category on the page.</summary>
public sealed class RotatingAchievementRow
{
    public RotatingAchievementRow(RotatingAchievement achievement)
    {
        Name = achievement.Name;
        IsDone = achievement.Done;
        ProgressText = !achievement.Done && achievement.Max > 1
            ? $"{Math.Min(achievement.Current, achievement.Max)} / {achievement.Max}"
            : "";
    }

    public string Name { get; }

    public bool IsDone { get; }

    /// <summary>E.g. "2 / 5" while under way; empty when done or a one-step achievement.</summary>
    public string ProgressText { get; }
}

/// <summary>
/// A rotating achievement category on the page (e.g. Daily Fractals): what's left first, then what's done, the first
/// few until expanded.
/// </summary>
public sealed class RotatingCategoryGroup : ViewModelBase
{
    private const int CollapsedCount = 6;

    private readonly IReadOnlyList<RotatingAchievementRow> _all;
    private bool _showAll;

    public RotatingCategoryGroup(RotatingCategory category, bool showAll)
    {
        Title = category.Name;
        Icon = category.Icon;
        Summary = $"{category.Done} of {category.Achievements.Count} done";
        _all = category.Achievements.Select(a => new RotatingAchievementRow(a)).OrderBy(r => r.IsDone).ToList();
        _showAll = showAll;
        ToggleCommand = new RelayCommand(() =>
        {
            _showAll = !_showAll;
            OnPropertyChanged(nameof(Rows));
            OnPropertyChanged(nameof(ToggleText));
            OnPropertyChanged(nameof(ShowAll));
        });
    }

    public string Title { get; }

    /// <summary>The category's icon (render URL), as in the game's achievement panel.</summary>
    public string Icon { get; }

    /// <summary>E.g. "2 of 15 done".</summary>
    public string Summary { get; }

    /// <summary>Expanded; kept by the page across rebuilds.</summary>
    public bool ShowAll => _showAll;

    public IReadOnlyList<RotatingAchievementRow> Rows => _showAll ? _all : _all.Take(CollapsedCount).ToList();

    public bool HasMore => _all.Count > CollapsedCount;

    public string ToggleText => _showAll ? "Show fewer" : $"Show all {_all.Count}";

    public RelayCommand ToggleCommand { get; }
}
