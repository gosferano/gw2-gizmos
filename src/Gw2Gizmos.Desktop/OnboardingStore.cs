using System;
using System.IO;
using System.Text.Json;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Remembers whether the one-time first-run onboarding has been shown. Persisted as a tiny JSON flag under the app
/// data dir (same pattern as the other small settings stores), so the welcome page appears only on the first launch
/// and never again once the user has finished or skipped it.
/// </summary>
public sealed class OnboardingStore
{
    private readonly string _path;
    private bool _completed;

    public OnboardingStore(AppPaths paths)
    {
        _path = paths.File("onboarding.json");
        _completed = Load();
    }

    public bool IsCompleted => _completed;

    public void MarkCompleted()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(new State { Completed = true }));
        }
        catch (Exception)
        {
            // Best-effort: failing to persist just means onboarding may show again next launch — not fatal.
        }
    }

    private bool Load()
    {
        try
        {
            return File.Exists(_path)
                && (JsonSerializer.Deserialize<State>(File.ReadAllText(_path))?.Completed ?? false);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private sealed record State
    {
        public bool Completed { get; init; }
    }
}
