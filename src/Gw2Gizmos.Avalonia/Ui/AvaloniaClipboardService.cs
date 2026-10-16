using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Avalonia.Ui;

/// <summary><see cref="IClipboardService"/> over the main window's clipboard; call on the UI thread.</summary>
public sealed class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        IClipboard? clipboard =
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
