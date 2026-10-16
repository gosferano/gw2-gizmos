using Gw2Gizmos.Desktop.Core.Platform;

namespace Gw2Gizmos.Avalonia.Platform.Linux;

/// <summary>
/// <see cref="IStartupRegistration"/> via an XDG autostart entry (<c>$XDG_CONFIG_HOME/autostart/&lt;name&gt;.desktop</c>),
/// honoured by every major Linux desktop session. The entry launches the app with <c>--minimized</c> so it starts in
/// the tray. The file name is per-build (dev vs release) so both can register without clobbering each other.
/// </summary>
public sealed class XdgAutostartRegistration : IStartupRegistration
{
    private readonly string _entryPath;

    public XdgAutostartRegistration(string entryName)
    {
        string configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } xdg
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        _entryPath = Path.Combine(configHome, "autostart", $"{entryName}.desktop");
    }

    public bool IsEnabled => File.Exists(_entryPath);

    public void SetEnabled(bool enabled)
    {
        try
        {
            if (!enabled)
            {
                File.Delete(_entryPath);
                return;
            }

            // An AppImage runs from a temporary mount; $APPIMAGE is the stable path of the image file itself.
            string? executable = Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } appImage
                ? appImage
                : Environment.ProcessPath;
            if (string.IsNullOrEmpty(executable))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_entryPath)!);
            File.WriteAllText(
                _entryPath,
                "[Desktop Entry]\n"
                    + "Type=Application\n"
                    + "Name=Gw2Gizmos\n"
                    + "Comment=Guild Wars 2 companion\n"
                    + $"Exec={QuoteExec(executable)} --minimized\n"
                    + "Terminal=false\n"
                    + "X-GNOME-Autostart-enabled=true\n");
        }
        catch (Exception)
        {
            // Best effort — an unwritable config dir just means the toggle won't persist.
        }
    }

    /// <summary>Quotes an Exec argument per the Desktop Entry spec (escape \, ", `, $ inside double quotes).</summary>
    private static string QuoteExec(string value)
    {
        var escaped = new System.Text.StringBuilder(value.Length + 2).Append('"');
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
}
