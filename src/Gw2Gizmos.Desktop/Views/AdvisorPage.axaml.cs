using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The Advisor hub: advice drawn from the account's own data, each section summarised.</summary>
public partial class AdvisorPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public AdvisorPage()
    {
        InitializeComponent();
    }

    public AdvisorPage(AdvisorViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
