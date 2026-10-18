using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary>
/// <see cref="IFolderLauncher"/> through Avalonia's launcher (the platform's own way of opening a folder), falling
/// back to the usual command where that doesn't open one: <c>explorer</c> on Windows, <c>xdg-open</c> on Linux.
/// </summary>
public sealed class AvaloniaFolderLauncher : IFolderLauncher
{
    public async Task<bool> OpenAsync(string path)
    {
        try
        {
            if ((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow is { } window
                && await window.StorageProvider.TryGetFolderFromPathAsync(path) is { } folder
                && await window.Launcher.LaunchFileAsync(folder))
            {
                return true;
            }
        }
        catch (Exception)
        {
            // Fall back to the command below.
        }

        return StartFileManager(path);
    }

    private static bool StartFileManager(string path)
    {
        string? command = OperatingSystem.IsWindows() ? "explorer.exe"
            : OperatingSystem.IsLinux() ? "xdg-open"
            : null;
        if (command is null)
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(command) { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);
            using Process? process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
