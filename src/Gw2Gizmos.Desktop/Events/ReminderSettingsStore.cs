using System;
using System.IO;
using System.Text.Json;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Stores how many minutes before an event a reminder fires. Persisted in a per-user file, cached in memory
/// and written through on change. The Events screen binds the lead-time dropdown to it;
/// <see cref="EventReminderService"/> reads it on each poll so a change applies live.
/// </summary>
public sealed class ReminderSettingsStore
{
    public const int DefaultLeadTimeMinutes = 5;

    private readonly string _path;
    private readonly object _gate = new();
    private int _leadTimeMinutes;

    public ReminderSettingsStore(AppPaths paths)
    {
        _path = paths.File("reminder-settings.json");
        _leadTimeMinutes = Load();
    }

    public int LeadTimeMinutes
    {
        get
        {
            lock (_gate)
            {
                return _leadTimeMinutes;
            }
        }
        set
        {
            lock (_gate)
            {
                if (_leadTimeMinutes == value)
                {
                    return;
                }

                _leadTimeMinutes = value;
                Save(value);
            }
        }
    }

    private sealed record Settings(int LeadTimeMinutes);

    private int Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return DefaultLeadTimeMinutes;
            }

            Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path));
            return settings is { LeadTimeMinutes: > 0 } ? settings.LeadTimeMinutes : DefaultLeadTimeMinutes;
        }
        catch (Exception)
        {
            return DefaultLeadTimeMinutes;
        }
    }

    private void Save(int minutes) => File.WriteAllText(_path, JsonSerializer.Serialize(new Settings(minutes)));
}
