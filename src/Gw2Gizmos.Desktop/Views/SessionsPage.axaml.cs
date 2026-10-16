using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The selected account's recorded play sessions, filterable by date and sortable.</summary>
public partial class SessionsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public SessionsPage()
    {
        InitializeComponent();
    }

    public SessionsPage(SessionsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
