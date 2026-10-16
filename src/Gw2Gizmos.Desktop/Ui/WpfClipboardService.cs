using System.Threading.Tasks;
using System.Windows;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary><see cref="IClipboardService"/> over WPF's <see cref="Clipboard"/>; call on the UI (STA) thread.</summary>
public sealed class WpfClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
    {
        Clipboard.SetText(text);
        return Task.CompletedTask;
    }
}
