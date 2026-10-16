using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class OnboardingPage : Page
{
    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
