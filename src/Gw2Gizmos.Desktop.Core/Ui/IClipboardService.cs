namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>The system clipboard; implemented by each UI head.</summary>
public interface IClipboardService
{
    /// <summary>Replaces the clipboard's contents with <paramref name="text"/>. May throw if the clipboard is locked.</summary>
    Task SetTextAsync(string text);
}
