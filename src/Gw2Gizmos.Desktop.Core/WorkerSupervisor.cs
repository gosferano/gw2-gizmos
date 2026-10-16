using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Keeps the ingestion worker running for the app's lifetime: starts it, and if it exits on its own (a crash, or
/// killed from outside) starts it again after a growing delay — 2 s, 5 s, 15 s, 1 min, then every 5 min — so a crash
/// loop doesn't hammer the machine or the log. A worker that stays up for <see cref="StableUptime"/> resets the delay.
/// <see cref="Stop"/> (app exit) kills it without restarting.
/// </summary>
public sealed class WorkerSupervisor
{
    private static readonly TimeSpan[] DefaultBackoff =
    {
        TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
    };

    private static readonly TimeSpan StableUptime = TimeSpan.FromMinutes(5);

    private readonly Func<Process?> _start;
    private readonly TimeSpan[] _backoff;
    private readonly ILogger<WorkerSupervisor> _logger;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stopping = new();
    private Process? _process;
    private DateTimeOffset _startedAt;
    private int _failures;

    public WorkerSupervisor(WorkerProcessLauncher launcher, ILogger<WorkerSupervisor> logger)
        : this(launcher.Start, DefaultBackoff, logger) { }

    internal WorkerSupervisor(Func<Process?> start, TimeSpan[] backoff, ILogger<WorkerSupervisor> logger)
    {
        _start = start;
        _backoff = backoff;
        _logger = logger;
    }

    /// <summary>Raised with each worker process started (the first and every restart) — e.g. to bind it to a job.</summary>
    public event Action<Process>? Started;

    /// <summary>Starts the worker; call once, at app startup.</summary>
    public void Start() => StartWorker();

    /// <summary>Stops the worker for good (app exit): cancels any pending restart and kills the running process.</summary>
    public void Stop()
    {
        Process? process;
        lock (_gate)
        {
            if (_stopping.IsCancellationRequested)
            {
                return;
            }

            _stopping.Cancel();
            process = _process;
            _process = null;
        }

        try
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop the worker process.");
        }
    }

    private void StartWorker()
    {
        lock (_gate)
        {
            if (_stopping.IsCancellationRequested)
            {
                return;
            }

            Process? process;
            try
            {
                process = _start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start the worker process.");
                process = null;
            }

            if (process is null)
            {
                // Not there (logged by the launcher) or failed to launch: nothing to supervise. A missing executable
                // won't appear by retrying, and a failed launch is logged; ingestion stays off until the app restarts.
                return;
            }

            _process = process;
            _startedAt = DateTimeOffset.UtcNow;
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => OnExited(process);
            Started?.Invoke(process);

            // Exited before the handler was attached: raise it now (Exited only fires for exits after subscription).
            if (process.HasExited)
            {
                ThreadPool.QueueUserWorkItem(_ => OnExited(process));
            }
        }
    }

    private void OnExited(Process process)
    {
        TimeSpan delay;
        lock (_gate)
        {
            // Stopped (app exiting), or a stale notification for a process already replaced.
            if (_stopping.IsCancellationRequested || !ReferenceEquals(process, _process))
            {
                return;
            }

            _process = null;
            if (DateTimeOffset.UtcNow - _startedAt >= StableUptime)
            {
                _failures = 0;
            }

            delay = _backoff[Math.Min(_failures, _backoff.Length - 1)];
            _failures++;
            _logger.LogWarning(
                "The worker exited unexpectedly (exit code {ExitCode}); restarting it in {Delay}.",
                SafeExitCode(process), delay);
        }

        _ = RestartAfterAsync(delay);
    }

    private async Task RestartAfterAsync(TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _stopping.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        StartWorker();
    }

    private static int? SafeExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
