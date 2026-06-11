using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Gw2Gizmos.Herald;

public partial class LogsPage : Page
{
    private ScrollViewer? _scrollViewer;
    private int _pendingShift;
    private bool _shiftQueued;

    public LogsPage(LogsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LogList.ApplyTemplate();
        _scrollViewer ??= FindScrollViewer(LogList);

        if (LogList.Items is INotifyCollectionChanged items)
        {
            items.CollectionChanged -= OnItemsChanged;
            items.CollectionChanged += OnItemsChanged;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_scrollViewer is null || e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex != 0)
        {
            return;
        }

        // At the top: let new (newest) items show. Scrolled down: keep the user's place by shifting
        // the offset down by however many rows were inserted above (item-based scrolling).
        if (_scrollViewer.VerticalOffset <= 0 && _pendingShift == 0)
        {
            return;
        }

        _pendingShift += e.NewItems?.Count ?? 0;

        if (_shiftQueued)
        {
            return;
        }

        _shiftQueued = true;
        Dispatcher.BeginInvoke(
            () =>
            {
                if (_scrollViewer is not null && _pendingShift > 0)
                {
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + _pendingShift);
                }

                _pendingShift = 0;
                _shiftQueued = false;
            },
            DispatcherPriority.Background
        );
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
