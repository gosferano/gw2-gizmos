using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Account › Shared inventory: the account-wide shared slots.</summary>
public partial class SharedInventoryPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public SharedInventoryPage()
    {
        InitializeComponent();
    }

    public SharedInventoryPage(SharedInventoryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
