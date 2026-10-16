using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>The events timer: world bosses, metas, instances and invasions with live countdowns and reminders.</summary>
public partial class EventsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public EventsPage()
    {
        InitializeComponent();
    }

    public EventsPage(EventsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
