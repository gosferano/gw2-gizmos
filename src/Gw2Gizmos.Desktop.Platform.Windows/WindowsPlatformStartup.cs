using Gw2Gizmos.Desktop.Core;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop.Platform.Windows;

/// <summary>
/// The Windows platform's startup work, run with the host — before the app creates its windows or starts the
/// worker: registers the process's AUMID (so toasts are attributed to the app) and binds every worker process the
/// supervisor starts, restarts included, to a kill-on-close job so none can outlive the app, even on a crash.
/// </summary>
internal sealed class WindowsPlatformStartup : IHostedService, IDisposable
{
    private readonly WorkerSupervisor _worker;
    private readonly ChildProcessJob _job = new();

    public WindowsPlatformStartup(WorkerSupervisor worker)
    {
        _worker = worker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        WindowsToastService.RegisterAppId();
        _worker.Started += process => _job.AddProcess(process);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Closing the job kills anything still in it — the backstop that guarantees the worker is gone.
    public void Dispose() => _job.Dispose();
}
