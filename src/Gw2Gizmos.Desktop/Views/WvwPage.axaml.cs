using Avalonia.Controls;
using Avalonia.Input;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The account's WvW matchup over the Mists map.</summary>
public partial class WvwPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public WvwPage()
    {
        InitializeComponent();
    }

    public WvwPage(WvwViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    /// <summary>Filters the flip feed to the tapped map (the rows are borders, not buttons, to keep their look).</summary>
    private void OnMapRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: WvwMapStatsRow row } && DataContext is WvwViewModel viewModel)
        {
            viewModel.SelectMapCommand.Execute(row);
        }
    }
}
