using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The Dashboard's Daily and weekly card: time to the daily reset, what's done of the Wizard's Vault, world bosses,
/// map chests and daily crafting, and the next world boss or meta still worth doing. Reloads after account syncs and
/// ticks every second; <see cref="Stop"/> when the Dashboard closes.
/// </summary>
public sealed class ResetProgressCardViewModel : ViewModelBase
{
    private readonly ResetProgressReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly IUiTimer _timer;
    private ResetProgressData _data = ResetProgressData.Empty;
    private string _dailyCountdown = "";
    private string _wizardsVault = "—";
    private string _worldBosses = "—";
    private string _mapChests = "—";
    private string _dailyCrafting = "—";
    private string _next = "";

    public ResetProgressCardViewModel(
        ResetProgressReader reader,
        SelectedAccountService account,
        DataChangeMonitor dataChanges,
        IUiDispatcher dispatcher
    )
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(account.AccountId) ? null : account.AccountId;
        Update();
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
        _timer = dispatcher.CreateTimer(TimeSpan.FromSeconds(1), Update);
        _timer.Start();
    }

    public void Stop()
    {
        _dataChanges.Dispose();
        _timer.Stop();
    }

    /// <summary>E.g. "5h 42m".</summary>
    public string DailyCountdown
    {
        get => _dailyCountdown;
        private set => SetProperty(ref _dailyCountdown, value);
    }

    /// <summary>E.g. "3/4 · 2/6": the daily and weekly objectives done.</summary>
    public string WizardsVault
    {
        get => _wizardsVault;
        private set => SetProperty(ref _wizardsVault, value);
    }

    /// <summary>E.g. "4/15".</summary>
    public string WorldBosses
    {
        get => _worldBosses;
        private set => SetProperty(ref _worldBosses, value);
    }

    public string MapChests
    {
        get => _mapChests;
        private set => SetProperty(ref _mapChests, value);
    }

    public string DailyCrafting
    {
        get => _dailyCrafting;
        private set => SetProperty(ref _dailyCrafting, value);
    }

    /// <summary>E.g. "Next: Claw of Jormag in 18:04"; empty when nothing scheduled is left.</summary>
    public string Next
    {
        get => _next;
        private set => SetProperty(ref _next, value);
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

        Update();
    }

    private void Update()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DailyCountdown = ResetProgressViewModel.Span(GameResets.NextDaily(now) - now);
        if (_data.IsEmpty)
        {
            return;
        }

        WizardsVault = string.Join(" · ", new[] { WizardsVaultPeriod.Daily, WizardsVaultPeriod.Weekly }
            .Select(period => ResetProgress.WizardsVault(_data, period, now))
            .Select(set => set is null || set.IsStale ? "—" : $"{set.Done}/{set.Objectives.Count}"));

        IReadOnlyList<ResetActivity> bosses = ResetProgress.Activities(_data, ResetActivityKind.WorldBoss, now);
        IReadOnlyList<ResetActivity> chests = ResetProgress.Activities(_data, ResetActivityKind.MapChest, now);
        WorldBosses = Count(bosses);
        MapChests = Count(chests);
        DailyCrafting = Count(ResetProgress.Activities(_data, ResetActivityKind.DailyCrafting, now));

        var next = bosses.Concat(chests)
            .Where(a => !a.Done && a.Event is not null)
            .Select(a => new { a.Name, Timing = EventTiming.Of(a.Event!, now) })
            .MinBy(a => a.Timing.SortKey);
        Next = next is null ? "" : $"Next: {next.Name} {next.Timing.Countdown}";
    }

    private static string Count(IReadOnlyList<ResetActivity> activities) =>
        activities.Count == 0 ? "—" : $"{activities.Count(a => a.Done)}/{activities.Count}";
}
