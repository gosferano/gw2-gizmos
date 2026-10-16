using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace Gw2Gizmos.MumbleLink.Bridge;

/// <summary>
/// Mirrors Guild Wars 2's MumbleLink block into a file, from inside the game's Wine/Proton prefix. On Linux the game's
/// "MumbleLink" shared memory lives in Wine, where a native reader can't open it; this runs in the same Wine session,
/// copies the block into <c>--output</c> (a Windows path onto the Linux filesystem, e.g. <c>Z:\home\…</c>) several times
/// a second, and exits shortly after the game does — so the prefix's wineserver can shut down and Steam sees the game
/// as closed. On exit the file is zeroed, which readers take as "game not running".
/// <para>
/// The game doesn't create "MumbleLink" itself: it checks for one every few seconds and writes into it once found
/// (normally Mumble or an overlay creates it). So the bridge creates it, and holds it for as long as it runs.
/// </para>
/// </summary>
internal static class Program
{
    private const string MapName = "MumbleLink";
    private const int BlockSize = 5460;
    private const string DefaultGameProcess = "Gw2-64";

    private static readonly TimeSpan CopyInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan GameCheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartupGrace = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ExitGrace = TimeSpan.FromSeconds(3);

    private static int Main(string[] args)
    {
        string? output = Option(args, "--output");
        string gameProcess = Option(args, "--game-process") ?? DefaultGameProcess;
        if (string.IsNullOrEmpty(output))
        {
            Console.Error.WriteLine("Usage: Gw2Gizmos.MumbleLink.Bridge --output <path> [--game-process <name>]");
            return 2;
        }

        using var outputStream = new FileStream(
            output, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        outputStream.SetLength(BlockSize);
        using var outputMap = MemoryMappedFile.CreateFromFile(
            outputStream, null, BlockSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
        using MemoryMappedViewAccessor outputView = outputMap.CreateViewAccessor(0, BlockSize);

        Console.Error.WriteLine($"Mirroring {MapName} to {output} while {gameProcess} runs.");
        byte[] block = new byte[BlockSize];
        MemoryMappedFile? gameMap = null;
        MemoryMappedViewAccessor? gameView = null;
        var started = Stopwatch.StartNew();
        var sinceGameCheck = Stopwatch.StartNew();
        TimeSpan? gameLastSeen = null;
        string? lastFailure = null;
        bool loggedFirstWrite = false;

        try
        {
            while (true)
            {
                if (sinceGameCheck.Elapsed >= GameCheckInterval || gameLastSeen is null)
                {
                    sinceGameCheck.Restart();
                    if (IsRunning(gameProcess))
                    {
                        gameLastSeen = started.Elapsed;
                    }
                    else if (gameLastSeen is { } seen ? started.Elapsed - seen > ExitGrace : started.Elapsed > StartupGrace)
                    {
                        Console.Error.WriteLine(gameLastSeen is null
                            ? $"{gameProcess} never started; exiting."
                            : $"{gameProcess} exited; exiting.");
                        return 0;
                    }
                }

                try
                {
                    if (gameView is null)
                    {
                        gameMap = MemoryMappedFile.CreateOrOpen(MapName, BlockSize, MemoryMappedFileAccess.ReadWrite);
                        gameView = gameMap.CreateViewAccessor(0, BlockSize, MemoryMappedFileAccess.Read);
                        Console.Error.WriteLine($"Created {MapName}; waiting for the game to write to it.");
                        lastFailure = null;
                    }

                    gameView.ReadArray(0, block, 0, BlockSize);
                    if (!loggedFirstWrite && BitConverter.ToUInt32(block, 4) != 0)
                    {
                        loggedFirstWrite = true;
                        Console.Error.WriteLine($"{gameProcess} is writing to {MapName}.");
                    }

                    outputView.WriteArray(0, block, 0, BlockSize);
                }
                catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException)
                {
                    // Retry on the next tick. Each new kind of failure is logged once, so a bridge that never gets the
                    // block says why.
                    if (ex.GetType().Name != lastFailure)
                    {
                        lastFailure = ex.GetType().Name;
                        Console.Error.WriteLine($"Can't create {MapName}: {ex.GetType().Name}: {ex.Message}");
                    }

                    gameView?.Dispose();
                    gameMap?.Dispose();
                    gameView = null;
                    gameMap = null;
                }

                Thread.Sleep(CopyInterval);
            }
        }
        finally
        {
            gameView?.Dispose();
            gameMap?.Dispose();
            // Readers treat an all-zero block as "not running", so the session ends now rather than on a frozen tick.
            outputView.WriteArray(0, new byte[BlockSize], 0, BlockSize);
            outputView.Flush();
        }
    }

    private static bool IsRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        foreach (Process process in processes)
        {
            process.Dispose();
        }

        return processes.Length > 0;
    }

    private static string? Option(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
