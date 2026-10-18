using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The account's WvW matchup over the Mists map.</summary>
public partial class WvwPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public WvwPage()
    {
        InitializeComponent();
    }

    public WvwPage(WvwViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
