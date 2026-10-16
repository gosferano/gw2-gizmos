using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Settings › Stored data: delete locally-stored data per type, per account.</summary>
public partial class StoredDataPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public StoredDataPage()
    {
        InitializeComponent();
    }

    public StoredDataPage(StoredDataViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
