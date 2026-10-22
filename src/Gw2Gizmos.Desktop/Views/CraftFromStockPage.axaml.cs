using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Advisor › Craft from stock: what to craft from the materials the account holds.</summary>
public partial class CraftFromStockPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public CraftFromStockPage()
    {
        InitializeComponent();
    }

    public CraftFromStockPage(CraftFromStockViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
