using System.Runtime.InteropServices;
using System.Security;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Shows Windows toast notifications using the built-in WinRT API (no third-party toolkit). For an
/// unpackaged app, toasts are attributed via an AppUserModelID; Windows requires a Start-Menu
/// shortcut carrying that same id to actually display them (see <see cref="EnsureShortcut"/> — TODO).
/// </summary>
internal static class ToastService
{
    private const string Aumid = "Gw2Gizmos.Herald";

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
