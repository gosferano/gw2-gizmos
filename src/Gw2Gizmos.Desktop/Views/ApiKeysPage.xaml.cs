using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class ApiKeysPage : Page
{
    public ApiKeysPage(ApiKeysViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
