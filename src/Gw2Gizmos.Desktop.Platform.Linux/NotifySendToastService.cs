using Gw2Gizmos.Desktop.Core.Platform;
using Gw2Gizmos.Desktop.Core.Ui;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// <see cref="IToastService"/> for Linux desktops via <c>notify-send</c> (libnotify), which talks to whatever
/// freedesktop notification daemon is running (GNOME, KDE, mako, dunst, swaync…). A "copy" notification carries an
/// action button; <c>--wait</c> keeps notify-send alive until the notification resolves and prints the chosen
/// action, which is then copied on the UI thread.
/// </summary>
internal sealed class NotifySendToastService : IToastService
{
    private const string Command = "notify-send";
    private const string CopyAction = "copy";

    private readonly IClipboardService _clipboard;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<NotifySendToastService> _logger;
    private bool _missingLogged;

    public NotifySendToastService(
        IClipboardService clipboard,
        IUiDispatcher dispatcher,
        ILogger<NotifySendToastService> logger
    )
    {
        _clipboard = clipboard;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public void Show(string title, string body, string? copyText = null, string? copyLabel = null) =>
        _ = ShowAsync(title, body, copyText, string.IsNullOrEmpty(copyLabel) ? "Copy" : copyLabel);

    private async Task ShowAsync(string title, string body, string? copyText, string copyLabel)
    {
        bool hasCopy = !string.IsNullOrEmpty(copyText);
        var arguments = new List<string> { "--app-name=Gw2Gizmos" };
        if (hasCopy)
        {
            arguments.Add($"--action={CopyAction}={copyLabel}");
            arguments.Add("--wait");
        }

        arguments.Add(title);
        arguments.Add(body);

        // A waiting notification can sit on screen (or in the daemon's history) for a long time.
        (int ExitCode, string Output)? result =
            await CommandRunner.RunAsync(Command, arguments, timeout: hasCopy ? TimeSpan.FromHours(1) : null);
        if (result is null)
        {
            if (!_missingLogged)
            {
                _missingLogged = true;
                _logger.LogWarning("Couldn't show a desktop notification: '{Command}' (libnotify) isn't available.", Command);
            }

            return;
        }

        if (hasCopy && result.Value.Output == CopyAction)
        {
            _dispatcher.Post(() => _ = CopyAsync(copyText!));
        }
    }

    private async Task CopyAsync(string text)
    {
        try
        {
            await _clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Copying from a notification failed.");
        }
    }
}
