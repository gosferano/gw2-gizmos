using System;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// One row on the Events screen: a <see cref="ScheduledEvent"/> plus its live countdown / active state,
/// recomputed each tick against the current UTC time by <see cref="EventsViewModel"/>.
/// </summary>
public sealed class EventRowViewModel : ViewModelBase
{
    private readonly ScheduledEvent _event;
    private readonly EventSubscriptionStore _subscriptions;
    private readonly EventFavoritesStore _favorites;
    private string _countdown = string.Empty;
    private bool _isActive;
    private bool _isSubscribed;
    private bool _isFavorite;
    private DateTimeOffset _sortKey;

    public EventRowViewModel(
        ScheduledEvent scheduledEvent,
        EventSubscriptionStore subscriptions,
        EventFavoritesStore favorites
    )
    {
        _event = scheduledEvent;
        _subscriptions = subscriptions;
        _favorites = favorites;
        _isSubscribed = subscriptions.IsSubscribed(scheduledEvent.Id);
        _isFavorite = favorites.IsFavorite(scheduledEvent.Id);
    }

    public string Name => _event.Name;
    public string Map => _event.Map;
    public string? ChatLink => _event.ChatLink;
    public bool HasChatLink => !string.IsNullOrEmpty(_event.ChatLink);

    /// <summary>The release this event belongs to — used by the expansion filter.</summary>
    public Expansion Expansion => _event.Expansion;

    /// <summary>Short tag shown on the row (Boss / Meta / Instance / Invasion).</summary>
    public string KindLabel => _event.Kind switch
    {
        EventKind.WorldBoss => "Boss",
        EventKind.MetaEvent => "Meta",
        EventKind.PublicInstance => "Instance",
        EventKind.Invasion => "Invasion",
        _ => string.Empty,
    };

    /// <summary>Active right now (drives top-of-list placement and the highlighted countdown).</summary>
    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    /// <summary>Whether the user wants a reminder before this event; persisted on change.</summary>
    public bool IsSubscribed
    {
        get => _isSubscribed;
        set
        {
            if (SetProperty(ref _isSubscribed, value))
            {
                _subscriptions.SetSubscribed(_event.Id, value);
            }
        }
    }

    /// <summary>Whether the event is starred (pinned to the top of the list); persisted on change.</summary>
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
            {
                _favorites.SetFavorite(_event.Id, value);
            }
        }
    }

    /// <summary>"in 1h 23m" / "5:30" when upcoming, "active · 12m left" when running.</summary>
    public string Countdown
    {
        get => _countdown;
        private set => SetProperty(ref _countdown, value);
    }

    /// <summary>Sort key: the end time while active, otherwise the next start. Notifies so live-sorting
    /// reorders the row when it rolls over to its next occurrence.</summary>
    public DateTimeOffset SortKey
    {
        get => _sortKey;
        private set => SetProperty(ref _sortKey, value);
    }

    public void Refresh(DateTimeOffset now)
    {
        DateTimeOffset? activeUntil = _event.ActiveUntilUtc(now);
        if (activeUntil is { } end)
        {
            IsActive = true;
            SortKey = end;
            Countdown = $"active · {Format(end - now)} left";
        }
        else
        {
            DateTimeOffset next = _event.NextStartUtc(now);
            IsActive = false;
            SortKey = next;
            Countdown = $"in {Format(next - now)}";
        }
    }

    // Hours+minutes when far out; minutes:seconds (ticking) when under an hour.
    private static string Format(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes:00}m"
            : $"{span.Minutes}:{span.Seconds:00}";
    }
}
