using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The worker launch carries the shared database and the config pipe, and finds the per-OS executable.</summary>
public sealed class WorkerProcessLauncherTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    [Fact]
    public void CreateStartInfo_ForwardsDatabaseAndPipe_AndRunsInDataDirectory()
    {
        var database = new DesktopDatabaseSettings("sqlite", "Data Source=/x.sqlite", "Data Source=/x.sqlite;Mode=ReadOnly");
        var launcher = new WorkerProcessLauncher(
            new AppPaths(_dataDirectory), database, "pipe-123", NullLogger<WorkerProcessLauncher>.Instance);

        ProcessStartInfo startInfo = launcher.CreateStartInfo();

        Assert.Equal(_dataDirectory, startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        // The worker writes, so it gets the read-write string, never the desktop's read-only one.
        Assert.Equal(
            new[]
            {
                "--ConnectionStrings:Gw2GizmosDb=Data Source=/x.sqlite",
                "--Database:Provider=sqlite",
                "--WorkerConfig:PipeName=pipe-123",
            },
            startInfo.ArgumentList);
    }

    [Fact]
    public void ExecutablePath_IsNextToTheApp_WithThePlatformExtension()
    {
        string expectedName = OperatingSystem.IsWindows()
            ? "Gw2Gizmos.Data.Worker.Cli.exe"
            : "Gw2Gizmos.Data.Worker.Cli";

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, expectedName), WorkerProcessLauncher.ExecutablePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }
}
