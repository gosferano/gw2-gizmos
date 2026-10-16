using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Sessions › a session › a segment: that character's loot.</summary>
public partial class SessionLootPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public SessionLootPage()
    {
        InitializeComponent();
    }

    public SessionLootPage(SessionLootViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
