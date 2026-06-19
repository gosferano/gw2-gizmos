using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Data.Worker.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Fires a reminder (toast + in-app feed, via the composite <see cref="INotifier"/>) shortly before each
/// event the user has subscribed to on the Events screen. Runs in the desktop process because that owns
/// Windows toasts. Each occurrence is notified at most once, tracked by its start time.
/// </summary>
public sealed class EventReminderService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

    private readonly EventSubscriptionStore _subscriptions;
    private readonly ReminderSettingsStore _settings;
    private readonly INotifier _notifier;
    private readonly ILogger<EventReminderService> _logger;
    private readonly IReadOnlyDictionary<string, ScheduledEvent> _events;
    private readonly Dictionary<string, DateTimeOffset> _lastNotifiedStart = new();

    public EventReminderService(
        EventSubscriptionStore subscriptions,
        ReminderSettingsStore settings,
        INotifier notifier,
        ILogger<EventReminderService> logger)
    {
        _subscriptions = subscriptions;
        _settings = settings;
        _notifier = notifier;
        _logger = logger;
        _events = WorldBosses.All
            .Concat(MetaEvents.All)
            .Concat(PublicInstances.All)
            .Concat(Invasions.All)
            .ToDictionary(e => e.Id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Event reminder service started ({Count} event(s) subscribed); lead time {LeadMinutes} min.",
            _subscriptions.SubscribedIds.Count,
            _settings.LeadTimeMinutes);

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                CheckOnce(DateTimeOffset.UtcNow);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }
            catch
            {
                // A bad tick shouldn't kill the loop; try again next interval.
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void CheckOnce(DateTimeOffset now)
    {
        // Read the lead time each pass so changes from the Events screen apply without a restart.
        TimeSpan leadTime = TimeSpan.FromMinutes(_settings.LeadTimeMinutes);

        foreach (string id in _subscriptions.SubscribedIds)
        {
            if (!_events.TryGetValue(id, out ScheduledEvent? scheduledEvent))
            {
                continue;
            }

            DateTimeOffset next = scheduledEvent.NextStartUtc(now);
            TimeSpan untilStart = next - now;
            if (untilStart > leadTime || untilStart <= TimeSpan.Zero)
            {
                continue;
            }

            // Don't re-fire for an occurrence we've already announced this pass through the window.
            if (_lastNotifiedStart.TryGetValue(id, out DateTimeOffset last) && last == next)
            {
                continue;
            }

            _lastNotifiedStart[id] = next;
            int minutes = Math.Max(1, (int)Math.Round(untilStart.TotalMinutes));
            _logger.LogInformation(
                "Reminder: {Event} starts in {Minutes} min on {Map}.",
                scheduledEvent.Name, minutes, scheduledEvent.Map);
            string title = $"{scheduledEvent.Name} starting soon";
            string body = $"Begins in {minutes} min on {scheduledEvent.Map}.";

            // Events with a fixed waypoint offer a one-click copy of its chat link straight from the toast;
            // those without (e.g. roaming invasions) get a plain reminder.
            if (!string.IsNullOrEmpty(scheduledEvent.ChatLink))
            {
                _notifier.Notify(title, body, "Events", scheduledEvent.ChatLink, "Copy waypoint");
            }
            else
            {
                _notifier.Notify(title, body, "Events");
            }
        }
    }
}
