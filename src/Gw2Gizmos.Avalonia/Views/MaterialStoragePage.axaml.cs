using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Account › Material storage: crafting materials by category.</summary>
public partial class MaterialStoragePage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public MaterialStoragePage()
    {
        InitializeComponent();
    }

    public MaterialStoragePage(MaterialStorageViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
