using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>
/// The achievements by the game's groups and categories, with the account's progress, filterable and sortable to
/// find quick wins.
/// </summary>
public partial class AchievementsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public AchievementsPage()
    {
        InitializeComponent();
    }

    public AchievementsPage(AchievementsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
