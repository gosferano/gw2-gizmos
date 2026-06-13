using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Tracks which scheduled events the user has opted into reminders for. Persisted as a JSON id list in a
/// per-user file (so it survives restarts), cached in memory and written through on each change. Read by the
/// Events screen's per-row bell toggle and by <see cref="EventReminderService"/>.
/// </summary>
public sealed class EventSubscriptionStore
{
    private readonly string _path;
    private readonly HashSet<string> _ids;
    private readonly object _gate = new();

    public EventSubscriptionStore(AppPaths paths)
    {
        _path = paths.File("event-subscriptions.json");
        _ids = Load();
    }

    public bool IsSubscribed(string eventId)
    {
        lock (_gate)
        {
            return _ids.Contains(eventId);
        }
    }

    /// <summary>A snapshot of the currently-subscribed event ids (safe to enumerate off-thread).</summary>
    public IReadOnlyCollection<string> SubscribedIds
    {
        get
        {
            lock (_gate)
            {
                return _ids.ToArray();
            }
        }
    }

    public void SetSubscribed(string eventId, bool subscribed)
    {
        lock (_gate)
        {
            bool changed = subscribed ? _ids.Add(eventId) : _ids.Remove(eventId);
            if (!changed)
            {
                return;
            }

            Save();
        }
    }

    private HashSet<string> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new HashSet<string>();
            }

            string[]? ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path));
            return ids is null ? new HashSet<string>() : new HashSet<string>(ids);
        }
        catch (Exception)
        {
            return new HashSet<string>();
        }
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_ids.ToArray()));
}
