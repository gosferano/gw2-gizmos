using System.Threading.Tasks;
using System.Windows;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Legacy.Ui;

/// <summary><see cref="IDialogService"/> over WPF's modal <see cref="MessageBox"/>.</summary>
public sealed class WpfDialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message) =>
        Task.FromResult(
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes);
}
