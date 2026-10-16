using System.Diagnostics;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Stops the worker when the desktop app that spawned it goes away — including a crash or a hard kill, where the
/// app never gets to stop it. Windows additionally binds the worker to a kill-on-close Job Object; this is the
/// cross-platform guarantee (Linux has no equivalent). The parent is identified by pid <em>and</em> start time, so a
/// pid the OS reuses for an unrelated process after the app exits doesn't keep the worker alive.
/// </summary>
public sealed class ParentProcessWatchdog : BackgroundService
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);

    private readonly int _parentProcessId;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ParentProcessWatchdog> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly Func<bool>? _isParentAliveOverride;

    public ParentProcessWatchdog(
        int parentProcessId,
        IHostApplicationLifetime lifetime,
        ILogger<ParentProcessWatchdog> logger
    )
        : this(parentProcessId, lifetime, logger, DefaultPollInterval, isParentAlive: null)
    {
    }

    /// <summary>Test seam: a fast poll and a scripted liveness probe instead of a real process.</summary>
    internal ParentProcessWatchdog(
        int parentProcessId,
        IHostApplicationLifetime lifetime,
        ILogger<ParentProcessWatchdog> logger,
        TimeSpan pollInterval,
        Func<bool>? isParentAlive
    )
    {
        _parentProcessId = parentProcessId;
        _lifetime = lifetime;
        _logger = logger;
        _pollInterval = pollInterval;
        _isParentAliveOverride = isParentAlive;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Func<bool> isParentAlive = _isParentAliveOverride ?? CreateProbe(_parentProcessId);
        _logger.LogInformation("Watching parent process {Pid}; the worker exits when it does.", _parentProcessId);

        using var timer = new PeriodicTimer(_pollInterval);
        try
        {
            while (isParentAlive())
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The worker is stopping anyway.
            return;
        }

        _logger.LogWarning("Parent process {Pid} has exited; stopping the worker.", _parentProcessId);
        _lifetime.StopApplication();
    }

    /// <summary>
    /// A probe that's true while the process that had <paramref name="processId"/> when the worker started is still
    /// running. Already gone at startup → always false.
    /// </summary>
    internal static Func<bool> CreateProbe(int processId)
    {
        DateTime? startTime = TryGetStartTime(processId);
        if (startTime is null)
        {
            return () => false;
        }

        return () => TryGetStartTime(processId) == startTime;
    }

    private static DateTime? TryGetStartTime(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            return process.HasExited ? null : process.StartTime;
        }
        catch (ArgumentException)
        {
            // No process with that id.
            return null;
        }
        catch (InvalidOperationException)
        {
            // Exited between lookup and inspection.
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Can't inspect it (access denied). Assume it's still the parent rather than kill a healthy worker.
            return DateTime.MinValue;
        }
    }
}
