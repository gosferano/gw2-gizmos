using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary><see cref="IDialogService"/> over FluentAvalonia's in-window <see cref="FAContentDialog"/>.</summary>
public sealed class AvaloniaDialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 },
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            // Destructive confirmations: Enter shouldn't delete by accident.
            DefaultButton = FAContentDialogButton.Close,
        };

        return await dialog.ShowAsync() == FAContentDialogResult.Primary;
    }
}
