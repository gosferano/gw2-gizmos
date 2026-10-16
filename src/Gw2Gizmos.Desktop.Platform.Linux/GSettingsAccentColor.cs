using System.Diagnostics;
using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// <see cref="ISystemAccentColor"/> from GNOME's <c>org.gnome.desktop.interface accent-color</c> — the accent GTK 4 /
/// libadwaita apps use, set by GNOME and by settings tools on other desktops (Hyprland, Sway, …). FluentAvalonia reads
/// the accent itself only on KDE, LXQt and LXDE, so there this stays out of the way; elsewhere it reads the named accent,
/// maps it to libadwaita's colour, and follows changes live via <c>gsettings monitor</c>.
/// </summary>
internal sealed class GSettingsAccentColor : ISystemAccentColor, IHostedService, IDisposable
{
    private const string Schema = "org.gnome.desktop.interface";
    private const string Key = "accent-color";

    /// <summary>GNOME's named accents and the colours libadwaita renders them with.</summary>
    private static readonly Dictionary<string, uint> Accents = new(StringComparer.Ordinal)
    {
        ["blue"] = 0xFF3584E4,
        ["teal"] = 0xFF2190A4,
        ["green"] = 0xFF3A944A,
        ["yellow"] = 0xFFC88800,
        ["orange"] = 0xFFED5B00,
        ["red"] = 0xFFE62D42,
        ["pink"] = 0xFFD56199,
        ["purple"] = 0xFF9141AC,
        ["slate"] = 0xFF6F8396,
    };

    private readonly ILogger<GSettingsAccentColor> _logger;
    private readonly object _gate = new();
    private uint? _current;
    private bool _read;
    private Process? _monitor;

    public GSettingsAccentColor(ILogger<GSettingsAccentColor> logger)
    {
        _logger = logger;
    }

    public event Action? Changed;

    public uint? Current
    {
        get
        {
            lock (_gate)
            {
                if (!_read)
                {
                    _read = true;
                    _current = ToolkitReadsAccent() ? null : Parse(CommandRunner.Run("gsettings", ["get", Schema, Key]));
                }

                return _current;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Only follow changes where there's an accent to follow (the key exists: GNOME 47+ schemas).
        if (Current is not null)
        {
            StartMonitor();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Process? monitor = Interlocked.Exchange(ref _monitor, null);
        if (monitor is null)
        {
            return;
        }

        try
        {
            if (!monitor.HasExited)
            {
                monitor.Kill();
            }
        }
        catch (Exception)
        {
            // Already gone.
        }

        monitor.Dispose();
    }

    private void StartMonitor()
    {
        try
        {
            var startInfo = new ProcessStartInfo("gsettings")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("monitor");
            startInfo.ArgumentList.Add(Schema);
            startInfo.ArgumentList.Add(Key);

            Process? monitor = Process.Start(startInfo);
            if (monitor is null)
            {
                return;
            }

            // Each change prints "accent-color: 'teal'".
            monitor.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not { } line || Parse((0, line[(line.IndexOf(':') + 1)..])) is not { } accent)
                {
                    return;
                }

                lock (_gate)
                {
                    if (accent == _current)
                    {
                        return;
                    }

                    _current = accent;
                }

                Changed?.Invoke();
            };
            monitor.BeginOutputReadLine();
            monitor.BeginErrorReadLine();
            _monitor = monitor;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Couldn't watch the GNOME accent colour; changes apply on the next start.");
        }
    }

    /// <summary>The colour for gsettings' output (<c>'blue'</c>), or null when unset, unknown or unreadable.</summary>
    private static uint? Parse((int ExitCode, string Output)? result) =>
        result is { ExitCode: 0, Output: { } output }
            && Accents.TryGetValue(output.Trim().Trim('\''), out uint accent)
                ? accent
                : null;

    /// <summary>Desktops whose accent FluentAvalonia reads itself (KDE's kdeglobals, LXQt, LXDE).</summary>
    private static bool ToolkitReadsAccent() =>
        (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "")
            .Split(':')
            .Any(desktop => desktop.Equals("KDE", StringComparison.OrdinalIgnoreCase)
                || desktop.Equals("LXQt", StringComparison.OrdinalIgnoreCase)
                || desktop.Equals("LXDE", StringComparison.OrdinalIgnoreCase));
}
