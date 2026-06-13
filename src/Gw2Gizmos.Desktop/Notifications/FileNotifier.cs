using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Notifications;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// <see cref="INotifier"/> that persists each notification to a per-user JSON file (capped to the most
/// recent <see cref="HistoryLimit"/>) and publishes it to the in-app feed. Replaces the shared-DB notifier
/// now that the desktop owns its own state; the ingestion database is the worker's alone. The list is kept
/// newest-first so the Notifications screen can load it as-is. <see cref="Notification"/> is reused purely as
/// a serialization shape here (no database involved).
/// </summary>
public sealed class FileNotifier : INotifier
{
    public const string SourceName = "Desktop";
    private const int HistoryLimit = 200;

    private readonly string _path;
    private readonly NotificationHub _hub;
    private readonly object _gate = new();

    public FileNotifier(AppPaths paths, NotificationHub hub)
    {
        _path = paths.File("notifications.json");
        _hub = hub;
    }

    public void Notify(string title, string message, string category = "General")
    {
        var notification = new Notification
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Source = SourceName,
            Category = category,
            Title = title,
            Body = message,
            IsRead = false,
        };

        lock (_gate)
        {
            List<Notification> history = Load();
            history.Insert(0, notification);
            if (history.Count > HistoryLimit)
            {
                history.RemoveRange(HistoryLimit, history.Count - HistoryLimit);
            }

            Save(history);
        }

        _hub.Publish(notification);
    }

    /// <summary>The persisted notification history, newest first.</summary>
    public IReadOnlyList<Notification> History()
    {
        lock (_gate)
        {
            return Load();
        }
    }

    private List<Notification> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new List<Notification>();
            }

            string json = File.ReadAllText(_path);
            return string.IsNullOrWhiteSpace(json)
                ? new List<Notification>()
                : JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
        }
        catch (Exception)
        {
            // A corrupt history file shouldn't break notifications; start fresh.
            return new List<Notification>();
        }
    }

    private void Save(List<Notification> history)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(history));
    }
}
