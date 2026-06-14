using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs Settings → Advanced: a per-sync interval picker (the four presets). Each change persists to the
/// <see cref="IntervalSettingsStore"/>, which the worker-config pipe pushes to the worker so it retimes its
/// loops. A standing caution on the page warns that changing defaults isn't recommended.
/// </summary>
public sealed class AdvancedSettingsViewModel : ViewModelBase
{
    public AdvancedSettingsViewModel(IntervalSettingsStore intervals)
    {
        foreach (WorkerSync sync in WorkerSyncs.All)
        {
            Syncs.Add(new IntervalRow(sync, intervals));
        }
    }

    public BreadcrumbEntry[] Breadcrumbs { get; } = new[]
    {
        new BreadcrumbEntry { Title = "Settings", Target = typeof(SettingsPage) },
        new BreadcrumbEntry { Title = "Advanced" },
    };

    public ObservableCollection<IntervalRow> Syncs { get; } = new();
}

/// <summary>One sync's interval row: its label and the selected preset (two-way, persisted on change).</summary>
public sealed class IntervalRow : ViewModelBase
{
    private readonly IntervalSettingsStore _store;
    private readonly string _key;
    private IntervalOption _selected;

    public IntervalRow(WorkerSync sync, IntervalSettingsStore store)
    {
        _store = store;
        _key = sync.Key;
        Display = sync.Display;
        TimeSpan current = store.GetInterval(sync.Key);
        _selected = IntervalOption.All.FirstOrDefault(option => option.Interval == current) ?? IntervalOption.All[0];
    }

    public string Display { get; }

    public IReadOnlyList<IntervalOption> Options => IntervalOption.All;

    public IntervalOption Selected
    {
        get => _selected;
        set
        {
            if (value is not null && SetProperty(ref _selected, value))
            {
                _store.SetInterval(_key, value.Interval);
            }
        }
    }
}

/// <summary>A selectable sync cadence preset.</summary>
public sealed record IntervalOption(string Label, TimeSpan Interval)
{
    public static IReadOnlyList<IntervalOption> All { get; } = new[]
    {
        new IntervalOption("1 day", TimeSpan.FromDays(1)),
        new IntervalOption("1 hour", TimeSpan.FromHours(1)),
        new IntervalOption("15 minutes", TimeSpan.FromMinutes(15)),
        new IntervalOption("5 minutes", TimeSpan.FromMinutes(5)),
    };

    public override string ToString() => Label;
}
