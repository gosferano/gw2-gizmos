using System.Diagnostics;
using System.Runtime.Versioning;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// Makes play-session tracking work for Guild Wars 2 under Wine/Proton. The game's MumbleLink shared memory only exists
/// inside Wine, so whenever the game is running (and Play sessions is on) this starts the bundled MumbleLink bridge in
/// the game's own Wine session — same wine build, same Wine settings, so same prefix and wineserver — and the bridge
/// mirrors the block into <see cref="FilePath"/> for the worker. The bridge exits by itself shortly after the game does;
/// nothing needs configuring in Steam.
/// <para>
/// Steam runs the game in its pressure-vessel container, and the wineserver (inside it) is what opens files, so
/// <see cref="FilePath"/> must be visible there: the data directory under $HOME is, $XDG_RUNTIME_DIR isn't.
/// </para>
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class ProtonMumbleLinkBridge : BackgroundService
{
    /// <summary>The Windows process name Wine gives the game (and its /proc comm).</summary>
    private const string GameProcessName = "Gw2-64.exe";

    private const string BridgeFileName = "Gw2Gizmos.MumbleLink.Bridge.exe";

    /// <summary>
    /// Wine's per-process plumbing in the game's environment (an inherited wineserver fd, preloader state) and
    /// container-only preloads; copied over, they'd break the bridge's own startup.
    /// </summary>
    private static readonly HashSet<string> ProcessOnlyVariables = new(StringComparer.Ordinal)
    {
        "WINESERVERSOCKET", "WINELOADERNOEXEC", "WINEPRELOADRESERVE", "WINE_LD_PRELOAD",
    };

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly FeatureSettingsStore _features;
    private readonly ILogger<ProtonMumbleLinkBridge> _logger;
    private int? _attachedGamePid;

    public ProtonMumbleLinkBridge(string filePath, FeatureSettingsStore features, ILogger<ProtonMumbleLinkBridge> logger)
    {
        FilePath = filePath;
        _features = features;
        _logger = logger;
    }

    /// <summary>Where the bridge mirrors the block (and the worker reads it).</summary>
    public string FilePath { get; }

    /// <summary>The bundled bridge, published next to the app by the build.</summary>
    public static string BridgePath => Path.Combine(AppContext.BaseDirectory, "mumblelink-bridge", BridgeFileName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                Poll();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MumbleLink bridge check failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void Poll()
    {
        if (!_features.IsEnabled(WorkerFeatures.PlaySessions.Key))
        {
            return;
        }

        if (FindGame() is not { } game)
        {
            _attachedGamePid = null;
            return;
        }

        if (_attachedGamePid == game.Pid)
        {
            return;
        }

        // One bridge per game run and build; an app restart mid-game finds the bridge its previous run started. A
        // bridge another build started (dev beside release) mirrors into that build's file, not ours, so it doesn't
        // count — each build runs its own, all sharing the game's one MumbleLink mapping.
        if (IsBridgeRunningFor(BridgeOutputArgument))
        {
            _logger.LogInformation(
                "Guild Wars 2 (pid {Pid}) already has this app's MumbleLink bridge running; using it.", game.Pid);
            _attachedGamePid = game.Pid;
            return;
        }

        if (!File.Exists(BridgePath))
        {
            _logger.LogWarning("MumbleLink bridge not found at {Path}; play sessions won't be tracked.", BridgePath);
            _attachedGamePid = game.Pid;
            return;
        }

        if (FindWine(game) is not { } wine)
        {
            _logger.LogWarning(
                "Couldn't find the wine binary running Guild Wars 2 (pid {Pid}); play sessions won't be tracked.", game.Pid);
            _attachedGamePid = game.Pid;
            return;
        }

        StartBridge(game, wine);
        _attachedGamePid = game.Pid;
    }

    private void StartBridge(GameProcess game, string wine)
    {
        var startInfo = new ProcessStartInfo(wine)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(BridgePath);
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(BridgeOutputArgument);

        // The game's Wine/Proton settings put the bridge in the same prefix and wine session (WINEPREFIX, sync mode,
        // DLL paths, …). The rest of its environment points into Steam's container, so ours is used for that.
        foreach ((string key, string value) in game.Environment)
        {
            if ((key.StartsWith("WINE", StringComparison.Ordinal) || key.StartsWith("PROTON_", StringComparison.Ordinal))
                && !ProcessOnlyVariables.Contains(key))
            {
                startInfo.Environment[key] = value;
            }
        }

        startInfo.Environment["WINEDEBUG"] = "-all";

        Process? process = Process.Start(startInfo);
        if (process is null)
        {
            _logger.LogWarning("Couldn't start the MumbleLink bridge.");
            return;
        }

        process.OutputDataReceived += (_, e) => LogBridgeLine(e.Data);
        process.ErrorDataReceived += (_, e) => LogBridgeLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _logger.LogInformation(
            "Started the MumbleLink bridge for Guild Wars 2 (pid {Pid}, prefix {Prefix}).",
            game.Pid, game.Environment.GetValueOrDefault("WINEPREFIX"));
    }

    private void LogBridgeLine(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _logger.LogInformation("MumbleLink bridge: {Line}", line);
        }
    }

    /// <summary>The running game: a Wine process named <see cref="GameProcessName"/> with a WINEPREFIX.</summary>
    private static GameProcess? FindGame()
    {
        foreach (string dir in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(dir), out int pid))
            {
                continue;
            }

            try
            {
                string comm = File.ReadAllText(Path.Combine(dir, "comm")).Trim();
                if (!string.Equals(comm, GameProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dictionary<string, string> environment = ReadEnvironment(Path.Combine(dir, "environ"));
                if (environment.ContainsKey("WINEPREFIX"))
                {
                    return new GameProcess(pid, environment, new FileInfo(Path.Combine(dir, "exe")).LinkTarget);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Exited mid-scan, or not ours to read.
            }
        }

        return null;
    }

    /// <summary>
    /// The wine binary of the game's Wine build (a different build couldn't join its wineserver): next to the game
    /// process's own executable (Proton: files/bin/wine), else in a Proton "files/bin", else WINELOADER.
    /// </summary>
    private static string? FindWine(GameProcess game)
    {
        var candidates = new List<string>();
        if (game.Executable is { } exe)
        {
            string? dir = Path.GetDirectoryName(exe);
            if (dir is not null)
            {
                candidates.Add(Path.Combine(dir, "wine"));
                candidates.Add(Path.Combine(dir, "wine64"));
            }

            int files = exe.IndexOf("/files/", StringComparison.Ordinal);
            if (files >= 0)
            {
                candidates.Add(exe[..files] + "/files/bin/wine");
            }
        }

        if (game.Environment.GetValueOrDefault("WINELOADER") is { Length: > 0 } loader)
        {
            candidates.Add(loader);
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>The bridge's <c>--output</c>: <see cref="FilePath"/> as a Windows path through Wine's Z: drive.</summary>
    private string BridgeOutputArgument => "Z:" + FilePath.Replace('/', '\\');

    /// <summary>
    /// Whether a bridge writing to <paramref name="output"/> is running. Wine keeps a Windows program's arguments in
    /// its <c>/proc/&lt;pid&gt;/cmdline</c>, so the bridge is matched by its exe name and its <c>--output</c>.
    /// </summary>
    private static bool IsBridgeRunningFor(string output)
    {
        foreach (string dir in Directory.EnumerateDirectories("/proc"))
        {
            try
            {
                if (!int.TryParse(Path.GetFileName(dir), out _))
                {
                    continue;
                }

                string cmdline = File.ReadAllText(Path.Combine(dir, "cmdline"));
                if (cmdline.Contains(BridgeFileName, StringComparison.Ordinal)
                    && cmdline.Contains(output, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Exited mid-scan.
            }
        }

        return false;
    }

    private static Dictionary<string, string> ReadEnvironment(string path)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in File.ReadAllText(path).Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = entry.IndexOf('=');
            if (separator > 0)
            {
                environment[entry[..separator]] = entry[(separator + 1)..];
            }
        }

        return environment;
    }

    private sealed record GameProcess(int Pid, Dictionary<string, string> Environment, string? Executable);
}
