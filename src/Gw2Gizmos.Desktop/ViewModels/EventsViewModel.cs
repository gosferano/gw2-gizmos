using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Events timer screen: every world boss, map meta, public instance, and invasion from the static
/// schedule, as live rows sorted soonest-first (active events float to the top). A one-second timer reticks
/// each row's countdown and re-orders the list as events roll over. Singleton, so the clock keeps running and
/// the list is built once.
/// </summary>
public sealed class EventsViewModel : ViewModelBase
{
    private readonly List<EventRowViewModel> _rows;
    private readonly DispatcherTimer _timer;

    public EventsViewModel(EventSubscriptionStore subscriptions)
    {
        _rows = WorldBosses.All
            .Concat(MetaEvents.All)
            .Concat(PublicInstances.All)
            .Concat(Invasions.All)
            .Select(e => new EventRowViewModel(e, subscriptions))
            .ToList();

        Refresh();
        foreach (EventRowViewModel row in SortedRows())
        {
            Events.Add(row);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    /// <summary>Live, sorted view of every scheduled event (bound to the page).</summary>
    public ObservableCollection<EventRowViewModel> Events { get; } = new();

    /// <summary>Copies an event's chat link to the clipboard so it can be pasted into the GW2 chat.</summary>
    public ICommand CopyChatLinkCommand { get; } = new RelayCommand<string>(CopyChatLink);

    private void Tick()
    {
        Refresh();

        // Countdowns updated in place above; only re-order when rollovers actually changed the sort order.
        List<EventRowViewModel> sorted = SortedRows().ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int current = Events.IndexOf(sorted[i]);
            if (current != i)
            {
                Events.Move(current, i);
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

    // Active events first (by when they end), then upcoming ones by soonest start.
    private IEnumerable<EventRowViewModel> SortedRows() =>
        _rows.OrderBy(r => r.IsActive ? 0 : 1).ThenBy(r => r.SortKey);

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
