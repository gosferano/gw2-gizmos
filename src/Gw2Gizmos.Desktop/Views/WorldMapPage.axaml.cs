using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>An interactive map of Tyria.</summary>
public partial class WorldMapPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public WorldMapPage()
    {
        InitializeComponent();
    }

    public WorldMapPage(WorldMapViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
