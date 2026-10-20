using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Daily and weekly page: time to the next daily and weekly reset, the Wizard's Vault's objectives, and
/// the world bosses, map chests, daily crafts and raid wings done this reset. Reloads after account syncs; a
/// one-second tick runs the countdowns, and the lists are rebuilt every half minute (to keep the soonest events
/// first) and as a reset passes (which clears what was done).
/// </summary>
public sealed class ResetProgressViewModel : ViewModelBase, INavigationAware
{
    private static readonly TimeSpan RebuildInterval = TimeSpan.FromSeconds(30);

    private readonly ResetProgressReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly IUiTimer _timer;
    private readonly IClipboardService _clipboard;
    private ResetProgressData _data = ResetProgressData.Empty;
    private DateTimeOffset _builtAt;
    private bool _isLoaded;
    private string _dailyCountdown = "";
    private string _weeklyCountdown = "";
    private string _dailyAt = "";
    private string _weeklyAt = "";
    private string _acclaimLeft = "";
    private IReadOnlyList<WizardsVaultGroup> _wizardsVault = [];
    private IReadOnlyList<ResetActivityGroupRow> _raidWings = [];
    private string _raidSummary = "";
    private IReadOnlyList<ResetActivityGroupRow> _dungeons = [];
    private string _dungeonSummary = "";
    private IReadOnlyList<RotatingCategoryGroup> _rotating = [];
    private readonly ReminderSettingsStore _eventSettings;

    public ResetProgressViewModel(
        ResetProgressReader reader,
        SelectedAccountService account,
        DataChangeMonitor dataChanges,
        IUiDispatcher dispatcher,
        IClipboardService clipboard,
        ReminderSettingsStore eventSettings
    )
    {
        _reader = reader;
        _eventSettings = eventSettings;
        _clipboard = clipboard;
        CopyChatLinkCommand = new RelayCommand<string>(chatLink => _ = CopyChatLinkAsync(chatLink));
        _accountId = string.IsNullOrEmpty(account.AccountId) ? null : account.AccountId;

        Rebuild(DateTimeOffset.UtcNow);
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
        _timer = dispatcher.CreateTimer(TimeSpan.FromSeconds(1), Tick);
        _timer.Start();
    }

    public void OnNavigatedFrom()
    {
        _dataChanges.Dispose();
        _timer.Stop();
    }

    /// <summary>Copies a world boss's or map meta's waypoint chat link.</summary>
    public RelayCommand<string> CopyChatLinkCommand { get; }

    /// <summary>Loaded and nothing synced: the page explains how to turn it on instead.</summary>
    public bool IsEmpty => _isLoaded && _data.IsEmpty;

    public bool HasData => !_data.IsEmpty;

    /// <summary>E.g. "5h 42m".</summary>
    public string DailyCountdown
    {
        get => _dailyCountdown;
        private set => SetProperty(ref _dailyCountdown, value);
    }

    /// <summary>The daily reset in local time, e.g. "at 03:00".</summary>
    public string DailyAt
    {
        get => _dailyAt;
        private set => SetProperty(ref _dailyAt, value);
    }

    /// <summary>E.g. "3d 10h".</summary>
    public string WeeklyCountdown
    {
        get => _weeklyCountdown;
        private set => SetProperty(ref _weeklyCountdown, value);
    }

    /// <summary>The weekly reset in local time, e.g. "Mon 10:30".</summary>
    public string WeeklyAt
    {
        get => _weeklyAt;
        private set => SetProperty(ref _weeklyAt, value);
    }

    /// <summary>The Astral Acclaim the current objectives and meta rewards still give.</summary>
    public string AcclaimLeft
    {
        get => _acclaimLeft;
        private set => SetProperty(ref _acclaimLeft, value);
    }

    public IReadOnlyList<WizardsVaultGroup> WizardsVault
    {
        get => _wizardsVault;
        private set => SetProperty(ref _wizardsVault, value);
    }

    public ResetActivitySection WorldBosses { get; } = new("World bosses", "today");

    public ResetActivitySection MapChests { get; } = new("Map chests", "today");

    public ResetActivitySection DailyCrafting { get; } = new("Daily crafting", "today");

    public IReadOnlyList<ResetActivityGroupRow> RaidWings
    {
        get => _raidWings;
        private set => SetProperty(ref _raidWings, value);
    }

    public IReadOnlyList<ResetActivityGroupRow> Dungeons
    {
        get => _dungeons;
        private set => SetProperty(ref _dungeons, value);
    }

    /// <summary>E.g. "1 of 8 dungeons today".</summary>
    public string DungeonSummary
    {
        get => _dungeonSummary;
        private set => SetProperty(ref _dungeonSummary, value);
    }

    /// <summary>
    /// The rotating achievement categories (Daily Fractals, rift hunts, …), and a festival's dailies while it's the
    /// festival chosen on the Events page.
    /// </summary>
    public IReadOnlyList<RotatingCategoryGroup> RotatingAchievements
    {
        get => _rotating;
        private set => SetProperty(ref _rotating, value);
    }

    public bool HasRotatingAchievements => RotatingAchievements.Count > 0;

    /// <summary>The rotating categories' cards in two columns, alternating, so expanding one only moves its own
    /// column.</summary>
    public IReadOnlyList<RotatingCategoryGroup> RotatingLeft =>
        RotatingAchievements.Where((_, i) => i % 2 == 0).ToList();

    public IReadOnlyList<RotatingCategoryGroup> RotatingRight =>
        RotatingAchievements.Where((_, i) => i % 2 == 1).ToList();

    /// <summary>E.g. "2 of 8 wings this week".</summary>
    public string RaidSummary
    {
        get => _raidSummary;
        private set => SetProperty(ref _raidSummary, value);
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

        _isLoaded = true;
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasData));
        Rebuild(DateTimeOffset.UtcNow);
    }

    private async Task CopyChatLinkAsync(string? chatLink)
    {
        if (string.IsNullOrEmpty(chatLink))
        {
            return;
        }

        try
        {
            await _clipboard.SetTextAsync(chatLink);
        }
        catch
        {
            // The clipboard is occasionally locked by another app; a failed copy isn't worth surfacing.
        }
    }

    private void Tick()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        // A reset since the last build clears what was done; otherwise reorder now and then as events come and go.
        if (GameResets.Next(_builtAt) <= now || now - _builtAt >= RebuildInterval)
        {
            Rebuild(now);
            return;
        }

        UpdateCountdowns(now);
        WorldBosses.Tick(now);
        MapChests.Tick(now);
    }

    private void Rebuild(DateTimeOffset now)
    {
        _builtAt = now;
        UpdateCountdowns(now);

        string[] periods = [WizardsVaultPeriod.Daily, WizardsVaultPeriod.Weekly, WizardsVaultPeriod.Special];
        List<WizardsVaultSet> sets = periods
            .Select(period => ResetProgress.WizardsVault(_data, period, now))
            .OfType<WizardsVaultSet>()
            .ToList();
        WizardsVault = sets.Select(set => new WizardsVaultGroup(set)).ToList();
        AcclaimLeft = sets.Sum(set => set.AcclaimLeft).ToString("N0", CultureInfo.CurrentCulture);

        WorldBosses.Set(ResetProgress.Activities(_data, ResetActivityKind.WorldBoss, now), now);
        MapChests.Set(ResetProgress.Activities(_data, ResetActivityKind.MapChest, now), now);
        DailyCrafting.Set(ResetProgress.Activities(_data, ResetActivityKind.DailyCrafting, now), now);

        IReadOnlyList<ResetActivityGroup> wings = ResetProgress.Raids(_data, now);
        RaidWings = wings.Select(wing => new ResetActivityGroupRow(wing)).ToList();
        RaidSummary = $"{wings.Count(w => w.Done)} of {wings.Count} wings this week";
        IReadOnlyList<ResetActivityGroup> dungeons = ResetProgress.Dungeons(_data, now);
        Dungeons = dungeons.Select(dungeon => new ResetActivityGroupRow(dungeon)).ToList();
        DungeonSummary = $"{dungeons.Count(d => d.Done)} of {dungeons.Count} dungeons today";

        // Rebuilt every half minute, so each category keeps whether it's expanded.
        HashSet<string> expanded = RotatingAchievements.Where(g => g.ShowAll).Select(g => g.Title).ToHashSet();
        RotatingAchievements = ResetProgress.RotatingAchievements(_data, _eventSettings.SelectedFestival, now)
            .Select(category => new RotatingCategoryGroup(category, expanded.Contains(category.Name)))
            .ToList();
        OnPropertyChanged(nameof(HasRotatingAchievements));
        OnPropertyChanged(nameof(RotatingLeft));
        OnPropertyChanged(nameof(RotatingRight));
    }

    private void UpdateCountdowns(DateTimeOffset now)
    {
        DateTimeOffset daily = GameResets.NextDaily(now);
        DateTimeOffset weekly = GameResets.NextWeekly(now);
        DailyCountdown = Span(daily - now);
        WeeklyCountdown = Span(weekly - now);
        DailyAt = "at " + daily.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
        WeeklyAt = weekly.ToLocalTime().ToString("ddd ", CultureInfo.CurrentCulture)
            + weekly.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
    }

    /// <summary>A time left, e.g. "3d 10h", "5h 42m" or "12m".</summary>
    public static string Span(TimeSpan left) =>
        left.TotalDays >= 1 ? $"{(int)left.TotalDays}d {left.Hours}h"
        : left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes:00}m"
        : $"{Math.Max(1, (int)Math.Ceiling(left.TotalMinutes))}m";
}
