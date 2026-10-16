using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Per-category on/off switches for desktop notifications.</summary>
public partial class NotificationsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public NotificationsPage()
    {
        InitializeComponent();
    }

    public NotificationsPage(NotificationsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
