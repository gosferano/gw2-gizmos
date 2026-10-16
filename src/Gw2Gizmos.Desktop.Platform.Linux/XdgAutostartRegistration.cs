using Gw2Gizmos.Desktop.Core.Platform;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// <see cref="IStartupRegistration"/> via an XDG autostart entry (<c>$XDG_CONFIG_HOME/autostart/&lt;name&gt;.desktop</c>),
/// honoured by every major Linux desktop session. The entry launches the app with <c>--minimized</c> so it starts in
/// the tray. The file name is per-build (dev vs release) so both can register without clobbering each other.
/// </summary>
internal sealed class XdgAutostartRegistration : IStartupRegistration
{
    private readonly string _entryPath;

    public XdgAutostartRegistration(string entryName)
    {
        _entryPath = Path.Combine(DesktopEntry.ConfigHome, "autostart", $"{entryName}.desktop");
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

            string? executable = DesktopEntry.LaunchPath;
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
                    + $"Exec={DesktopEntry.QuoteExec(executable)} --minimized\n"
                    + "Terminal=false\n"
                    + "X-GNOME-Autostart-enabled=true\n");
        }
        catch (Exception)
        {
            // Best effort — an unwritable config dir just means the toggle won't persist.
        }
    }
}
