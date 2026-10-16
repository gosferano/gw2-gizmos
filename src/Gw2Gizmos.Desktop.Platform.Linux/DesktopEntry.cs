using System.Text;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>Helpers for freedesktop <c>.desktop</c> entries (launcher and autostart) and the XDG base directories.</summary>
internal static class DesktopEntry
{
    /// <summary>The window class the app sets (X11), matched by <c>StartupWMClass</c> so launchers group its window.</summary>
    public const string WindowClass = "Gw2Gizmos";

    /// <summary>The AppImage file the app runs from (<c>$APPIMAGE</c>; it executes from a temporary mount), or null.</summary>
    public static string? AppImagePath =>
        Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } path && File.Exists(path) ? path : null;

    /// <summary>What a desktop entry should launch: the AppImage file when there is one, else this executable.</summary>
    public static string? LaunchPath => AppImagePath ?? Environment.ProcessPath;

    /// <summary><c>$XDG_DATA_HOME</c>, usually <c>~/.local/share</c>.</summary>
    public static string DataHome => XdgDirectory("XDG_DATA_HOME", ".local", "share");

    /// <summary><c>$XDG_CONFIG_HOME</c>, usually <c>~/.config</c>.</summary>
    public static string ConfigHome => XdgDirectory("XDG_CONFIG_HOME", ".config");

    /// <summary>Quotes an Exec argument per the Desktop Entry spec (escape \, ", `, $ inside double quotes).</summary>
    public static string QuoteExec(string value)
    {
        var escaped = new StringBuilder(value.Length + 2).Append('"');
        foreach (char c in value)
        {
            if (c is '\\' or '"' or '`' or '$')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        return escaped.Append('"').ToString();
    }

    private static string XdgDirectory(string variable, params string[] fallbackUnderHome) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } path
            ? path
            : Path.Combine([Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), .. fallbackUnderHome]);
}
