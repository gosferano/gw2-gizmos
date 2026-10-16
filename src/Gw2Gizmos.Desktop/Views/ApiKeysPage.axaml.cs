using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The API keys page: add a key per account, see each key's permissions, pick the active account.</summary>
public partial class ApiKeysPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public ApiKeysPage()
    {
        InitializeComponent();
    }

    public ApiKeysPage(ApiKeysViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
