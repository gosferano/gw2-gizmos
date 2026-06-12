using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class EventsPage : Page
{
    public EventsPage(EventsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
