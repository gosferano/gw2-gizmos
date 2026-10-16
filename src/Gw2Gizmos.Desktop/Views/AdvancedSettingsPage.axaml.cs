using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Settings › Advanced: per-sync refresh intervals.</summary>
public partial class AdvancedSettingsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public AdvancedSettingsPage()
    {
        InitializeComponent();
    }

    public AdvancedSettingsPage(AdvancedSettingsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
