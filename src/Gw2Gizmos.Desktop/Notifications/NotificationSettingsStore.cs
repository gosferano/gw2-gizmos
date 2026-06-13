using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Per-category global enable flags for notifications, persisted to a per-user JSON file. A category absent
/// from the file defaults to enabled, so newly added categories are on until the user turns them off. The
/// Notifications settings page binds toggles to this; <see cref="NotificationDispatcher"/> checks it before
/// firing a toast.
/// </summary>
public sealed class NotificationSettingsStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly Dictionary<string, bool> _enabled;

    public NotificationSettingsStore(AppPaths paths)
    {
        _path = paths.File("notification-settings.json");
        _enabled = Load();
    }

    public bool IsEnabled(string category)
    {
        lock (_gate)
        {
            return !_enabled.TryGetValue(category, out bool enabled) || enabled;
        }
    }

    public void SetEnabled(string category, bool enabled)
    {
        lock (_gate)
        {
            _enabled[category] = enabled;
            Save();
        }
    }

    private Dictionary<string, bool> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new Dictionary<string, bool>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(_path))
                ?? new Dictionary<string, bool>();
        }
        catch (Exception)
        {
            return new Dictionary<string, bool>();
        }
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_enabled));
}
