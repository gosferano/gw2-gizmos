namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>Modal prompts the view-models raise; implemented by each UI head.</summary>
public interface IDialogService
{
    /// <summary>Asks the user to confirm a destructive action; true when they choose Yes.</summary>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>Shows the price alert dialog over <paramref name="editor"/>; true when the user saves.</summary>
    Task<bool> EditPriceAlertAsync(PriceAlertEditorViewModel editor);
}
