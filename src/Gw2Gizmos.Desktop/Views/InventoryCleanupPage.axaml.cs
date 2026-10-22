using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Advisor › Inventory cleanup: what to stack, deposit, open or sell.</summary>
public partial class InventoryCleanupPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public InventoryCleanupPage()
    {
        InitializeComponent();
    }

    public InventoryCleanupPage(InventoryCleanupViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
