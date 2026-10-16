using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The Events screen's settings: how many minutes before an event a reminder fires, and which festival (if any) the
/// user says is running. Persisted in a per-user file, cached in memory and written through on change. The Events
/// screen binds both; <see cref="EventReminderService"/> reads them on each poll so a change applies live.
/// </summary>
public sealed class ReminderSettingsStore
{
    public const int DefaultLeadTimeMinutes = 5;

    // The festival is stored by name ("DragonBash"), so the file is readable and survives enum reordering.
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    private readonly string _path;
    private readonly object _gate = new();
    private Settings _settings;

    public ReminderSettingsStore(AppPaths paths)
    {
        _path = paths.File("reminder-settings.json");
        _settings = Load();
    }

    public int LeadTimeMinutes
    {
        get
        {
            lock (_gate)
            {
                return _settings.LeadTimeMinutes;
            }
        }
        set => Update(_settings with { LeadTimeMinutes = value });
    }

    /// <summary>The festival whose events are shown and reminded about; null when none is running.</summary>
    public Festival? SelectedFestival
    {
        get
        {
            lock (_gate)
            {
                return _settings.Festival;
            }
        }
        set => Update(_settings with { Festival = value });
    }

    // Festival is optional so files written before it existed still load.
    private sealed record Settings(int LeadTimeMinutes, Festival? Festival = null);

    private void Update(Settings updated)
    {
        lock (_gate)
        {
            if (updated == _settings)
            {
                return;
            }

            _settings = updated;
            File.WriteAllText(_path, JsonSerializer.Serialize(updated, JsonOptions));
        }
    }

    private Settings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new Settings(DefaultLeadTimeMinutes);
            }

            Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path), JsonOptions);
            return settings is null
                ? new Settings(DefaultLeadTimeMinutes)
                : settings with { LeadTimeMinutes = settings.LeadTimeMinutes > 0 ? settings.LeadTimeMinutes : DefaultLeadTimeMinutes };
        }
        catch (Exception)
        {
            return new Settings(DefaultLeadTimeMinutes);
        }
    }
}
