using System.Runtime.InteropServices;
using System.Security;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Shows Windows toast notifications using the built-in WinRT API (no third-party toolkit). Toasts are
/// attributed via an AppUserModelID; Windows only displays them if a Start-Menu shortcut carries that same
/// id. The Velopack installer stamps it on the shortcut via <c>vpk pack --aumid Gw2Gizmos</c> (see the
/// release workflow), matching the id this process registers below.
/// </summary>
internal static class ToastService
{
    private const string Aumid = "Gw2Gizmos";

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    /// <summary>Associates this process with the app's AUMID so its toasts are attributed correctly.</summary>
    public static void RegisterAppId() => SetCurrentProcessExplicitAppUserModelID(Aumid);

    public static void Show(string title, string body)
    {
        var xml = new XmlDocument();
        xml.LoadXml(
            "<toast><visual><binding template=\"ToastGeneric\">"
                + $"<text>{SecurityElement.Escape(title)}</text>"
                + $"<text>{SecurityElement.Escape(body)}</text>"
                + "</binding></visual></toast>"
        );

        ToastNotificationManager.CreateToastNotifier(Aumid).Show(new ToastNotification(xml));
    }
}
