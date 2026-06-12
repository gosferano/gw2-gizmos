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
    private string _countdown = string.Empty;
    private bool _isActive;

    public EventRowViewModel(ScheduledEvent scheduledEvent)
    {
        _event = scheduledEvent;
    }

    public string Name => _event.Name;
    public string Map => _event.Map;
    public string? ChatLink => _event.ChatLink;
    public bool HasChatLink => !string.IsNullOrEmpty(_event.ChatLink);

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

    /// <summary>"in 1h 23m" / "5:30" when upcoming, "active · 12m left" when running.</summary>
    public string Countdown
    {
        get => _countdown;
        private set => SetProperty(ref _countdown, value);
    }

    /// <summary>Sort key: the end time while active, otherwise the next start.</summary>
    public DateTimeOffset SortKey { get; private set; }

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
