using System;
using Microsoft.Win32;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Reads and writes the per-user "launch at Windows startup" registration (the <c>HKCU…\Run</c> key). When
/// enabled, Windows starts the desktop at login with <c>--minimized</c> so it comes up in the tray; the desktop
/// then spawns the background worker as usual. The value name is per-build (dev vs release) so both can register
/// independently without clobbering each other. Best-effort: a locked-down registry just means the toggle won't
/// persist, never a crash.
/// </summary>
public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _valueName;

    public StartupRegistration(string valueName)
    {
        _valueName = valueName;
    }

    /// <summary>Whether the app is registered to launch at login.</summary>
    public bool IsEnabled
    {
        get
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(_valueName) is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        string? exePath = Environment.ProcessPath;
        if (enabled && string.IsNullOrEmpty(exePath))
        {
            return;
        }

        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                // Quote the path (spaces) and start in the tray.
                key.SetValue(_valueName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(_valueName, throwOnMissingValue: false);
            }
        }
        catch (Exception)
        {
            // Best effort — the registry may be locked down; the toggle simply won't persist.
        }
    }
}
