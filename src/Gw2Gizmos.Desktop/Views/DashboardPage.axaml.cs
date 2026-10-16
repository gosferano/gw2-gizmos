using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The startup page: API key, account, play-session, worker and app status cards.</summary>
public partial class DashboardPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public DashboardPage()
    {
        InitializeComponent();
    }

    public DashboardPage(DashboardViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
