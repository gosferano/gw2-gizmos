using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Advisor › Goals: the items the account plans to make or buy, and what they still need.</summary>
public partial class GoalsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public GoalsPage()
    {
        InitializeComponent();
    }

    public GoalsPage(GoalsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
