using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The WvW page's settings: the span its activity counts (kills, deaths and flips lately) cover. Persisted in a
/// per-user file, cached in memory and written through on change.
/// </summary>
public sealed class WvwSettingsStore
{
    /// <summary>The spans to pick from, in minutes.</summary>
    public static readonly int[] ActivityWindowChoices = [5, 10, 15, 30];

    public const int DefaultActivityWindowMinutes = 10;

    private readonly string _path;
    private readonly object _gate = new();
    private Settings _settings;

    public WvwSettingsStore(AppPaths paths)
    {
        _path = paths.File("wvw-settings.json");
        _settings = Load();
    }

    /// <summary>The span the activity counts cover, one of <see cref="ActivityWindowChoices"/>.</summary>
    public int ActivityWindowMinutes
    {
        get
        {
            lock (_gate)
            {
                return _settings.ActivityWindowMinutes;
            }
        }
        set => Update(_settings with { ActivityWindowMinutes = Valid(value) });
    }

    private sealed record Settings(int ActivityWindowMinutes);

    private static int Valid(int minutes) =>
        ActivityWindowChoices.Contains(minutes) ? minutes : DefaultActivityWindowMinutes;

    private void Update(Settings updated)
    {
        lock (_gate)
        {
            if (updated == _settings)
            {
                return;
            }

            _settings = updated;
            File.WriteAllText(_path, JsonSerializer.Serialize(updated));
        }
    }

    private Settings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new Settings(DefaultActivityWindowMinutes);
            }

            Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path));
            return new Settings(Valid(settings?.ActivityWindowMinutes ?? DefaultActivityWindowMinutes));
        }
        catch (Exception)
        {
            return new Settings(DefaultActivityWindowMinutes);
        }
    }
}
