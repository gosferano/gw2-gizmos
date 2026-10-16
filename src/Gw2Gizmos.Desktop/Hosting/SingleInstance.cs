using System.IO.Pipes;

namespace Gw2Gizmos.Desktop.Hosting;

/// <summary>
/// Keeps the app to one instance per user (per build: dev and release are separate). A second launch — e.g. from the
/// app launcher while the app sits in the tray — asks the running instance over a local pipe to show its window, then
/// exits, instead of starting a duplicate (which would also spawn a second worker and double every notification).
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string ShowCommand = "show";

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stopping = new();

    private SingleInstance(Mutex mutex, string pipeName)
    {
        _mutex = mutex;
        _pipeName = pipeName;
    }

    /// <summary>Claims the instance for <paramref name="appId"/>; null when another instance already holds it.</summary>
    public static SingleInstance? TryAcquire(string appId)
    {
        var mutex = new Mutex(initiallyOwned: false, MutexName(appId));
        bool owned;
        try
        {
            owned = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance crashed while holding it; it's ours now.
            owned = true;
        }

        if (!owned)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, PipeName(appId));
    }

    /// <summary>Asks the running instance to show its window; false when it couldn't be reached.</summary>
    public static bool SignalExisting(string appId)
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".", PipeName(appId), PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(TimeSpan.FromSeconds(2));
            using var writer = new StreamWriter(client);
            writer.WriteLine(ShowCommand);
            return true;
        }
        catch (Exception)
        {
            // An older instance without the pipe, or one still starting up: nothing to do but exit.
            return false;
        }
    }

    /// <summary>Calls <paramref name="onShowRequested"/> (on a background thread) whenever another launch asks.</summary>
    public void ListenForShowRequests(Action onShowRequested)
    {
        CancellationToken stopping = _stopping.Token;
        _ = Task.Run(async () =>
        {
            while (!stopping.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        _pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(stopping);
                    using var reader = new StreamReader(server);
                    if (await reader.ReadLineAsync(stopping) == ShowCommand)
                    {
                        onShowRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (IOException)
                {
                    // A launch that disconnected mid-message; wait for the next one.
                }
            }
        }, stopping);
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _mutex.Dispose();
    }

    // Windows: a Local\ name already spans the user's logon session. Elsewhere .NET scopes Local\ to the Unix session,
    // and launchers start apps in sessions of their own — so a per-user Global\ name.
    private static string MutexName(string appId) =>
        OperatingSystem.IsWindows()
            ? $@"Local\{appId}.SingleInstance"
            : $@"Global\{appId}.SingleInstance.{Environment.UserName}";

    private static string PipeName(string appId) => $"{appId}.Activate.{Environment.UserName}";
}
