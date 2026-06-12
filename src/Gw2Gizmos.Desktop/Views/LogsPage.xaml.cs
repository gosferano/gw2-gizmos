using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gw2Gizmos.Desktop;

public partial class LogsPage : Page
{
    private ScrollViewer? _scrollViewer;

    public LogsPage(LogsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LogList.ApplyTemplate();

        if (_scrollViewer is not null)
        {
            return;
        }

        _scrollViewer = FindScrollViewer(LogList);
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    // Newest entries are prepended at the top of the grid. When the user has scrolled down into the
    // history, a row inserted above the viewport shifts every visible row down by one. Rather than let
    // that shift render and correct the offset a frame later (which flickers), we compensate inside the
    // same ScrollChanged pass that reports the extent growth: bumping the offset by exactly the amount
    // the extent grew keeps the visible rows pinned in place, so nothing appears to move.
    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only react to content added above the viewport (extent grew). At the very top the user is
        // following the newest entries, so let them flow in instead of holding the old position.
        if (e.ExtentHeightChange <= 0 || e.VerticalOffset <= 0)
        {
            return;
        }

        _scrollViewer!.ScrollToVerticalOffset(e.VerticalOffset + e.ExtentHeightChange);
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer scrollViewer)
        {
            return scrollViewer;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            ScrollViewer? found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
