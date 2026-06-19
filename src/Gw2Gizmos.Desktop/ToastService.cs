using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
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

    // A toast with an Activated handler must stay referenced until it resolves, or the GC can collect the
    // managed wrapper and the click is silently dropped. Hold live ones here; release on click/dismiss/fail.
    private static readonly HashSet<ToastNotification> Live = new();
    private static readonly object Gate = new();

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    /// <summary>Associates this process with the app's AUMID so its toasts are attributed correctly.</summary>
    public static void RegisterAppId() => SetCurrentProcessExplicitAppUserModelID(Aumid);

    public static void Show(string title, string body) => Show(title, body, null, null);

    /// <param name="copyText">When set, the toast carries an action button that copies this text to the
    /// clipboard on click (in-process; works while the app is running). Null shows a plain toast.</param>
    /// <param name="copyLabel">Label for that button; defaults to "Copy".</param>
    public static void Show(string title, string body, string? copyText, string? copyLabel)
    {
        bool hasCopy = !string.IsNullOrEmpty(copyText);
        string label = string.IsNullOrEmpty(copyLabel) ? "Copy" : copyLabel!;

        var xml = new XmlDocument();
        xml.LoadXml(
            "<toast><visual><binding template=\"ToastGeneric\">"
                + $"<text>{SecurityElement.Escape(title)}</text>"
                + $"<text>{SecurityElement.Escape(body)}</text>"
                + "</binding></visual>"
                + (hasCopy
                    ? "<actions>"
                        + $"<action content=\"{SecurityElement.Escape(label)}\" arguments=\"copy\" activationType=\"foreground\"/>"
                        + "</actions>"
                    : string.Empty)
                + "</toast>"
        );

        var toast = new ToastNotification(xml);

        if (hasCopy)
        {
            string text = copyText!;
            toast.Activated += (sender, args) =>
            {
                // Only the copy button copies; a body click (empty arguments) is left free for future use.
                if (args is ToastActivatedEventArgs activated && activated.Arguments == "copy")
                {
                    CopyToClipboard(text);
                }

                Release(sender);
            };
            toast.Dismissed += (sender, _) => Release(sender);
            toast.Failed += (sender, _) => Release(sender);

            lock (Gate)
            {
                Live.Add(toast);
            }
        }

        ToastNotificationManager.CreateToastNotifier(Aumid).Show(toast);
    }

    private static void Release(ToastNotification toast)
    {
        lock (Gate)
        {
            Live.Remove(toast);
        }
    }

    private static void CopyToClipboard(string text)
    {
        // Activated fires on a background thread; clipboard access must run on the STA UI thread.
        Application? app = Application.Current;
        app?.Dispatcher.Invoke(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch
            {
                // The clipboard is occasionally locked by another app; a failed copy isn't worth surfacing.
            }
        });
    }
}
