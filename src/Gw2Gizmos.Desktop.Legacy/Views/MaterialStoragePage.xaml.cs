using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class MaterialStoragePage : Page
{
    public MaterialStoragePage(MaterialStorageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
