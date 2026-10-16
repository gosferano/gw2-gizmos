using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>The in-app log viewer: recent lines from the desktop and the worker, filterable by level, source and text.</summary>
public partial class LogsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public LogsPage()
    {
        InitializeComponent();
    }

    public LogsPage(LogsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
