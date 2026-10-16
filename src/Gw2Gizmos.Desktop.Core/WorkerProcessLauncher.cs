using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Starts the heavy ingestion engine as a separate worker process (so it can't stall the UI). The worker ships as a
/// sibling executable in the app's own directory, sharing the runtime + common dependencies (see the CopyWorkerCli /
/// PublishWorkerCli targets in the UI head's csproj). The worker watches this process and exits with it; a head may
/// bind it more tightly (the WPF app's kill-on-close Job Object).
/// </summary>
public sealed class WorkerProcessLauncher
{
    /// <summary>The worker's executable name without the platform extension.</summary>
    public const string ExecutableName = "Gw2Gizmos.Data.Worker.Cli";

    private readonly AppPaths _paths;
    private readonly DesktopDatabaseSettings _database;
    private readonly string _workerConfigPipeName;
    private readonly string? _mumbleLinkFilePath;
    private readonly ILogger<WorkerProcessLauncher> _logger;

    /// <param name="mumbleLinkFilePath">Where a MumbleLink bridge mirrors the game's shared memory (Linux, game under
    /// Wine); null to let the worker read the game's shared memory directly (Windows).</param>
    public WorkerProcessLauncher(
        AppPaths paths,
        DesktopDatabaseSettings database,
        string workerConfigPipeName,
        ILogger<WorkerProcessLauncher> logger,
        string? mumbleLinkFilePath = null
    )
    {
        _paths = paths;
        _database = database;
        _workerConfigPipeName = workerConfigPipeName;
        _mumbleLinkFilePath = mumbleLinkFilePath;
        _logger = logger;
    }

    /// <summary>Where the worker executable is expected: next to the app, with the platform's extension.</summary>
    public static string ExecutablePath =>
        Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? $"{ExecutableName}.exe" : ExecutableName);

    /// <summary>
    /// The worker's launch: the same provider + connection string so it opens the identical database, the config
    /// pipe name so it can fetch keys/features/intervals live (no secret ever touches the command line), and this
    /// process's id so the worker exits when the app does, even after a crash.
    /// </summary>
    public ProcessStartInfo CreateStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            WorkingDirectory = _paths.DataDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add($"--ConnectionStrings:Gw2GizmosDb={_database.WorkerConnectionString}");
        startInfo.ArgumentList.Add($"--Database:Provider={_database.Provider}");
        startInfo.ArgumentList.Add($"--WorkerConfig:PipeName={_workerConfigPipeName}");
        startInfo.ArgumentList.Add($"--WorkerConfig:ParentProcessId={Environment.ProcessId}");
        if (_mumbleLinkFilePath is not null)
        {
            startInfo.ArgumentList.Add($"--MumbleLink:FilePath={_mumbleLinkFilePath}");
        }
        return startInfo;
    }

    /// <summary>Starts the worker; null (logged) when its executable isn't there, so ingestion just won't run.</summary>
    public Process? Start()
    {
        ProcessStartInfo startInfo = CreateStartInfo();
        if (!File.Exists(startInfo.FileName))
        {
            _logger.LogWarning("Worker process not found at {Path}; data ingestion will not run.", startInfo.FileName);
            return null;
        }

        Process? process = Process.Start(startInfo);
        if (process is not null)
        {
            _logger.LogInformation("Started ingestion worker process (pid {Pid}).", process.Id);
        }

        return process;
    }
}
