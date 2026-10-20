using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>
/// What's done and what's left this reset: the Wizard's Vault, world bosses, map chests, daily crafting and raids,
/// with the time to the next resets.
/// </summary>
public partial class ResetProgressPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public ResetProgressPage()
    {
        InitializeComponent();
    }

    public ResetProgressPage(ResetProgressViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
