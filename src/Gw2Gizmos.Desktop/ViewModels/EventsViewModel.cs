using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Events timer screen: every world boss, map meta, public instance, and invasion from the static
/// schedule as live rows. A one-second timer reticks each row's countdown; a <see cref="ListCollectionView"/>
/// with live sorting keeps them ordered (favorites pinned, then active, then soonest) and applies the search +
/// expansion filter. Singleton, so the clock keeps running and the list is built once.
/// </summary>
public sealed class EventsViewModel : ViewModelBase
{
    private readonly List<EventRowViewModel> _rows;
    private readonly ListCollectionView _view;
    private readonly ReminderSettingsStore _reminderSettings;
    private readonly DispatcherTimer _timer;
    private string _searchText = string.Empty;

    public EventsViewModel(
        EventSubscriptionStore subscriptions,
        EventFavoritesStore favorites,
        ReminderSettingsStore reminderSettings
    )
    {
        _reminderSettings = reminderSettings;
        _rows = WorldBosses.All
            .Concat(MetaEvents.All)
            .Concat(PublicInstances.All)
            .Concat(Invasions.All)
            .Select(e => new EventRowViewModel(e, subscriptions, favorites))
            .ToList();

        Refresh();

        // Build the filter chips before the view: setting Filter below triggers an immediate refresh that
        // calls FilterRow, which reads ExpansionFilters.
        ExpansionFilters = BuildExpansionFilters();

        // Favorites pinned, then active events, then soonest-first. Live sorting reorders a row as its
        // IsFavorite/IsActive/SortKey change (the per-second tick) without a full reset or flicker.
        _view = new ListCollectionView(_rows) { IsLiveSorting = true, Filter = FilterRow };
        _view.LiveSortingProperties.Add(nameof(EventRowViewModel.IsFavorite));
        _view.LiveSortingProperties.Add(nameof(EventRowViewModel.IsActive));
        _view.LiveSortingProperties.Add(nameof(EventRowViewModel.SortKey));
        _view.SortDescriptions.Add(new SortDescription(nameof(EventRowViewModel.IsFavorite), ListSortDirection.Descending));
        _view.SortDescriptions.Add(new SortDescription(nameof(EventRowViewModel.IsActive), ListSortDirection.Descending));
        _view.SortDescriptions.Add(new SortDescription(nameof(EventRowViewModel.SortKey), ListSortDirection.Ascending));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    /// <summary>Live, sorted + filtered view of every scheduled event (bound to the page).</summary>
    public ICollectionView Events => _view;

    /// <summary>Lead-time choices (minutes) for the "remind me before" dropdown.</summary>
    public IReadOnlyList<int> LeadTimeOptions { get; } = new[] { 5, 10, 15 };

    /// <summary>The expansion / Living-World filter items; none selected means show all.</summary>
    public IReadOnlyList<ExpansionFilter> ExpansionFilters { get; }

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
    public ICommand CopyChatLinkCommand { get; } = new RelayCommand<string>(CopyChatLink);

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
        foreach (EventRowViewModel row in _rows)
        {
            row.Refresh(now);
        }
    }

    private bool FilterRow(object item)
    {
        if (item is not EventRowViewModel row)
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

        // No expansion chips selected → show all; otherwise only the selected releases.
        bool anySelected = ExpansionFilters.Any(f => f.IsSelected);
        return !anySelected || ExpansionFilters.Any(f => f.IsSelected && f.Expansion == row.Expansion);
    }

    // Only expansions actually present in the data, in release (enum-declaration) order.
    private IReadOnlyList<ExpansionFilter> BuildExpansionFilters()
    {
        HashSet<Expansion> present = _rows.Select(r => r.Expansion).ToHashSet();
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

    private static void CopyChatLink(string? chatLink)
    {
        if (string.IsNullOrEmpty(chatLink))
        {
            return;
        }

        try
        {
            Clipboard.SetText(chatLink);
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
