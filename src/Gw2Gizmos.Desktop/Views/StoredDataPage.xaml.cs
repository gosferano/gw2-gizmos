using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class StoredDataPage : Page
{
    public StoredDataPage(StoredDataViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
