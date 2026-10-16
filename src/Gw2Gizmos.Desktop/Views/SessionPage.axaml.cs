using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Sessions › a session: its value, character segments and aggregated loot.</summary>
public partial class SessionPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public SessionPage()
    {
        InitializeComponent();
    }

    public SessionPage(SessionViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
