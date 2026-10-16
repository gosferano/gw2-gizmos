using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>The one-time welcome page: create an API key, tick the permissions, paste it, connect.</summary>
public partial class OnboardingPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public OnboardingPage()
    {
        InitializeComponent();
    }

    public OnboardingPage(OnboardingViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
