using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The market grid: every tradeable or craftable item with prices and craft economics, plus the selected
/// item's price history and craft tree.</summary>
public partial class ItemsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public ItemsPage()
    {
        InitializeComponent();
    }

    public ItemsPage(ItemsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
