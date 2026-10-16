using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The supervisor restarts a worker that exits on its own, and never one stopped for app exit.</summary>
public sealed class WorkerSupervisorTests
{
    private static readonly TimeSpan[] FastBackoff = { TimeSpan.FromMilliseconds(10) };

    // `dotnet --version` exits straight away — a stand-in for a worker that crashes on startup.
    private static Process? StartShortLived() =>
        Process.Start(new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true });

    [Fact]
    public async Task Restarts_a_worker_that_exits_on_its_own()
    {
        int starts = 0;
        var restarted = new TaskCompletionSource();
        var supervisor = new WorkerSupervisor(
            () =>
            {
                if (Interlocked.Increment(ref starts) == 3)
                {
                    restarted.TrySetResult();
                }

                return StartShortLived();
            },
            FastBackoff,
            NullLogger<WorkerSupervisor>.Instance);

        supervisor.Start();
        await restarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        supervisor.Stop();

        Assert.True(starts >= 3);
    }

    [Fact]
    public async Task Raises_Started_for_every_process()
    {
        var started = new List<Process>();
        var twice = new TaskCompletionSource();
        var supervisor = new WorkerSupervisor(StartShortLived, FastBackoff, NullLogger<WorkerSupervisor>.Instance);
        supervisor.Started += process =>
        {
            lock (started)
            {
                started.Add(process);
                if (started.Count == 2)
                {
                    twice.TrySetResult();
                }
            }
        };

        supervisor.Start();
        await twice.Task.WaitAsync(TimeSpan.FromSeconds(30));
        supervisor.Stop();

        Assert.NotSame(started[0], started[1]);
    }

    [Fact]
    public async Task Does_not_restart_after_Stop()
    {
        int starts = 0;
        var supervisor = new WorkerSupervisor(
            () =>
            {
                Interlocked.Increment(ref starts);
                return StartShortLived();
            },
            new[] { TimeSpan.FromMilliseconds(200) },
            NullLogger<WorkerSupervisor>.Instance);

        supervisor.Start();
        supervisor.Stop();
        await Task.Delay(1000);

        Assert.Equal(1, starts);
    }

    [Fact]
    public void Gives_up_quietly_when_the_worker_cannot_be_started()
    {
        int starts = 0;
        var supervisor = new WorkerSupervisor(
            () =>
            {
                starts++;
                return null;
            },
            FastBackoff,
            NullLogger<WorkerSupervisor>.Instance);

        supervisor.Start();
        supervisor.Stop();

        Assert.Equal(1, starts);
    }
}
