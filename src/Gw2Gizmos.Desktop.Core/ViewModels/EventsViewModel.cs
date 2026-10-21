using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Events timer screen: every world boss, map meta, public instance, and invasion from the static
/// schedule as live rows, plus the events of the festival the user marks as running. A one-second timer reticks each
/// row's countdown; a live-sorted <see cref="IItemsView"/> keeps them ordered (favorites pinned, then active, then
/// soonest) and applies the search, expansion and festival filters. Singleton, so the clock keeps running and the
/// list is built once.
/// </summary>
public sealed class EventsViewModel : ViewModelBase
{
    private readonly List<EventRowViewModel> _rows;
    private readonly IItemsView _view;
    private readonly ReminderSettingsStore _reminderSettings;
    private readonly IClipboardService _clipboard;
    private readonly IUiTimer _timer;
    private readonly CompletedEvents _completed;
    private bool _hasCompletionData;
    private string _searchText = string.Empty;

    public EventsViewModel(
        EventSubscriptionStore subscriptions,
        EventFavoritesStore favorites,
        ReminderSettingsStore reminderSettings,
        IItemsViewFactory views,
        IUiDispatcher dispatcher,
        IClipboardService clipboard,
        PageTargets targets,
        CompletedEvents completed
    )
    {
        _completed = completed;
        // Opened on a particular event (e.g. from the global search): start filtered to it.
        _searchText = targets.TakeEventSearch() ?? string.Empty;
        _reminderSettings = reminderSettings;
        _clipboard = clipboard;
        CopyChatLinkCommand = new RelayCommand<string>(chatLink => _ = CopyChatLinkAsync(chatLink));
        _rows = EventSchedule.All
            .Select(e => new EventRowViewModel(e, subscriptions, favorites))
            .ToList();

        FestivalOptions = new[] { new FestivalOption(null, "No festival") }
            .Concat(Enum.GetValues<Festival>().Select(f => new FestivalOption(f, Festivals.DisplayName(f))))
            .ToList();

        Refresh();

        // Build the filter chips before the view: setting Filter below triggers an immediate refresh that
        // calls FilterRow, which reads ExpansionFilters.
        ExpansionFilters = BuildExpansionFilters();

        // Favorites pinned, then active events, then soonest-first. Live sorting reorders a row as its
        // IsFavorite/IsActive/SortKey change (the per-second tick) without a full reset or flicker.
        _view = views.CreateLiveSorted(
            _rows,
            new[]
            {
                new ItemsViewSort(nameof(EventRowViewModel.IsFavorite), Descending: true),
                new ItemsViewSort(nameof(EventRowViewModel.IsActive), Descending: true),
                new ItemsViewSort(nameof(EventRowViewModel.SortKey)),
            }
        );
        _view.Filter = FilterRow;

        _timer = dispatcher.CreateTimer(TimeSpan.FromSeconds(1), Refresh);
        _timer.Start();
    }

    /// <summary>Live, sorted + filtered view of every scheduled event (bound to the page).</summary>
    public IItemsView Events => _view;

    /// <summary>Lead-time choices (minutes) for the "remind me before" dropdown.</summary>
    public IReadOnlyList<int> LeadTimeOptions { get; } = new[] { 5, 10, 15 };

    /// <summary>The expansion / Living-World filter items; none selected means show all.</summary>
    public IReadOnlyList<ExpansionFilter> ExpansionFilters { get; }

    /// <summary>"No festival" plus each festival with timed events, for the festival dropdown.</summary>
    public IReadOnlyList<FestivalOption> FestivalOptions { get; }

    /// <summary>
    /// The festival the user says is running (nothing reports festival dates): its events join the list and their
    /// reminders fire. Persisted, and read live by the reminder service.
    /// </summary>
    public FestivalOption SelectedFestival
    {
        get => FestivalOptions.First(option => option.Festival == _reminderSettings.SelectedFestival);
        set
        {
            if (value is not null && value.Festival != _reminderSettings.SelectedFestival)
            {
                _reminderSettings.SelectedFestival = value.Festival;
                OnPropertyChanged();
                _view.Refresh();
            }
        }
    }

    /// <summary>Label for the expansion-filter dropdown, reflecting how many releases are selected.</summary>
    public string ExpansionFilterLabel
    {
        get
        {
            int selected = ExpansionFilters.Count(f => f.IsSelected);
            return selected == 0 ? "All expansions" : $"{selected} expansion{(selected == 1 ? "" : "s")}";
        }
    }

    /// <summary>Copies an event's chat link to the clipboard so it can be pasted into the GW2 chat.</summary>
    public ICommand CopyChatLinkCommand { get; }

    /// <summary>Minutes before an event a reminder fires; persisted and read live by the reminder service.</summary>
    public int SelectedLeadTimeMinutes
    {
        get => _reminderSettings.LeadTimeMinutes;
        set
        {
            if (_reminderSettings.LeadTimeMinutes != value)
            {
                _reminderSettings.LeadTimeMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Hide the events done this reset; persisted, off by default.</summary>
    public bool HideDone
    {
        get => _reminderSettings.HideDone;
        set
        {
            if (_reminderSettings.HideDone != value)
            {
                _reminderSettings.HideDone = value;
                OnPropertyChanged();
                _view.Refresh();
            }
        }
    }

    /// <summary>
    /// Whether the account's completions are synced (the Daily and weekly progress feature, with the progression
    /// permission); without them nothing reads as done, so the toggle is hidden.
    /// </summary>
    public bool HasCompletionData
    {
        get => _hasCompletionData;
        private set => SetProperty(ref _hasCompletionData, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _view.Refresh();
            }
        }
    }

    private void Refresh()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlySet<string> done = _completed.DoneEventIds(now);
        HasCompletionData = _completed.HasData;
        bool doneChanged = false;
        foreach (EventRowViewModel row in _rows)
        {
            row.Refresh(now);
            bool isDone = done.Contains(row.Id);
            doneChanged |= row.IsDone != isDone;
            row.IsDone = isDone;
        }

        // Done ones come and go as syncs land and the reset passes; the list follows when it's hiding them.
        if (doneChanged && HideDone)
        {
            _view?.Refresh();
        }
    }

    private bool FilterRow(object item)
    {
        if (item is not EventRowViewModel row)
        {
            return false;
        }

        if (HideDone && row.IsDone)
        {
            return false;
        }

        string query = _searchText.Trim();
        if (query.Length > 0
            && row.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
            && row.Map.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
            && row.KindLabel.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        // Festival events: only the selected festival's, whatever the expansion filter (a festival is its own grouping).
        if (row.Festival is { } festival)
        {
            return festival == _reminderSettings.SelectedFestival;
        }

        // No expansion chips selected → show all; otherwise only the selected releases.
        bool anySelected = ExpansionFilters.Any(f => f.IsSelected);
        return !anySelected || ExpansionFilters.Any(f => f.IsSelected && f.Expansion == row.Expansion);
    }

    // Only expansions actually present in the (non-festival) data, in release (enum-declaration) order.
    private IReadOnlyList<ExpansionFilter> BuildExpansionFilters()
    {
        HashSet<Expansion> present = _rows.Where(r => r.Festival is null).Select(r => r.Expansion).ToHashSet();
        return Enum.GetValues<Expansion>()
            .Where(present.Contains)
            .Select(e => new ExpansionFilter(e, ExpansionDisplayName(e), OnExpansionFilterChanged))
            .ToList();
    }

    private void OnExpansionFilterChanged()
    {
        _view.Refresh();
        OnPropertyChanged(nameof(ExpansionFilterLabel));
    }

    private static string ExpansionDisplayName(Expansion expansion) => expansion switch
    {
        Expansion.CoreTyria => "Core Tyria",
        Expansion.LivingWorldSeason2 => "Living World S2",
        Expansion.HeartOfThorns => "Heart of Thorns",
        Expansion.PathOfFire => "Path of Fire",
        Expansion.LivingWorldSeason4 => "Living World S4",
        Expansion.IcebroodSaga => "Icebrood Saga",
        Expansion.EndOfDragons => "End of Dragons",
        Expansion.SecretsOfTheObscure => "Secrets of the Obscure",
        Expansion.JanthirWilds => "Janthir Wilds",
        Expansion.VisionsOfEternity => "Visions of Eternity",
        _ => expansion.ToString(),
    };

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
}

/// <summary>One expansion/Living-World toggle chip in the Events filter; refreshes the list when toggled.</summary>
public sealed class ExpansionFilter : ViewModelBase
{
    private readonly Action _onChanged;
    private bool _isSelected;

    public ExpansionFilter(Expansion expansion, string name, Action onChanged)
    {
        Expansion = expansion;
        Name = name;
        _onChanged = onChanged;
    }

    public Expansion Expansion { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onChanged();
            }
        }
    }
}

/// <summary>One entry in the festival dropdown: a festival, or "No festival" (<see cref="Festival"/> null).</summary>
public sealed record FestivalOption(Festival? Festival, string Name);
