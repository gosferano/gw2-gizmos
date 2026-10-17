using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Ui;
using Gw2Gizmos.Desktop.Views;

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

    public async Task<bool> EditPriceAlertAsync(PriceAlertEditorViewModel editor)
    {
        var dialog = new FAContentDialog
        {
            Title = editor.Title,
            Content = new PriceAlertEditorView { DataContext = editor },
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = FAContentDialogButton.Primary,
            DataContext = editor,
        };
        // Save is only possible with an amount entered.
        dialog.Bind(FAContentDialog.IsPrimaryButtonEnabledProperty, new Binding(nameof(PriceAlertEditorViewModel.IsValid)));

        return await dialog.ShowAsync() == FAContentDialogResult.Primary;
    }
}
