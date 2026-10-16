namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>Modal prompts the view-models raise; implemented by each UI head.</summary>
public interface IDialogService
{
    /// <summary>Asks the user to confirm a destructive action; true when they choose Yes.</summary>
    Task<bool> ConfirmAsync(string title, string message);
}
