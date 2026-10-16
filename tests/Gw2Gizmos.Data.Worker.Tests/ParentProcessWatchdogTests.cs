using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// The worker must stop once the app that spawned it is gone (a crash or hard kill included), and must not stop
/// while it's alive.
/// </summary>
public sealed class ParentProcessWatchdogTests
{
    [Fact]
    public async Task StopsTheWorker_OnceTheParentIsGone()
    {
        var lifetime = new RecordingLifetime();
        int probes = 0;
        // Alive for the first few polls, then gone.
        var watchdog = NewWatchdog(lifetime, () => Interlocked.Increment(ref probes) <= 3);

        await watchdog.StartAsync(CancellationToken.None);
        await lifetime.Stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(probes >= 4);
        await watchdog.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task KeepsTheWorkerRunning_WhileTheParentIsAlive()
    {
        var lifetime = new RecordingLifetime();
        var watchdog = NewWatchdog(lifetime, () => true);

        await watchdog.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await watchdog.StopAsync(CancellationToken.None);

        Assert.False(lifetime.Stopped.Task.IsCompleted);
    }

    [Fact]
    public void Probe_IsTrueForARunningProcess_AndFalseOnceItExits()
    {
        if (OperatingSystem.IsWindows())
        {
            // Covered by the platform-neutral tests above; the real-process case uses a Unix 'sleep'.
            return;
        }

        using Process process = Process.Start(new ProcessStartInfo("sleep", "30") { UseShellExecute = false })!;
        Func<bool> probe = ParentProcessWatchdog.CreateProbe(process.Id);
        Assert.True(probe());

        process.Kill();
        process.WaitForExit();
        Assert.False(probe());
    }

    [Fact]
    public void Probe_IsFalseForAProcessThatIsAlreadyGone()
    {
        using Process process = Process.Start(new ProcessStartInfo("dotnet", "--version")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
        })!;
        process.WaitForExit();

        Assert.False(ParentProcessWatchdog.CreateProbe(process.Id)());
    }

    private static ParentProcessWatchdog NewWatchdog(IHostApplicationLifetime lifetime, Func<bool> isParentAlive) =>
        new(
            parentProcessId: 0,
            lifetime,
            NullLogger<ParentProcessWatchdog>.Instance,
            TimeSpan.FromMilliseconds(10),
            isParentAlive);

    private sealed class RecordingLifetime : IHostApplicationLifetime
    {
        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => Stopped.TrySetResult();
    }
}
