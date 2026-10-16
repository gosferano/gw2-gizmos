using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Threading;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Avalonia.Ui;

/// <summary>
/// Backs <see cref="IItemsView"/> with the DataGrid package's <see cref="DataGridCollectionView"/>. That class is
/// sealed, so the view-model gets a thin wrapper instead; the wrapper implements
/// <see cref="IDataGridCollectionViewFactory"/>, which makes a bound <c>DataGrid</c> adopt the inner view directly
/// (native column-header sorting, incremental updates). Other items controls (a ListBox) see it as a read-only
/// <see cref="IList"/> raising the inner view's change notifications — Avalonia only accepts a change-notifying items
/// source that is also an <see cref="IList"/>.
/// </summary>
public sealed class AvaloniaItemsViewFactory : IItemsViewFactory
{
    public IItemsView Create(IList source) => new AvaloniaItemsView(new DataGridCollectionView(source));

    public IItemsView CreateLiveSorted(IList source, IReadOnlyList<ItemsViewSort> sorts) =>
        new LiveSortedItemsView(source, sorts);

    private class AvaloniaItemsView : IItemsView, IList, IDataGridCollectionViewFactory, INotifyCollectionChanged
    {
        private Predicate<object>? _filter;

        public AvaloniaItemsView(DataGridCollectionView view)
        {
            View = view;
        }

        protected DataGridCollectionView View { get; }

        public Predicate<object>? Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                View.Filter = value is null ? null : item => value(item);
            }
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => View.CollectionChanged += value;
            remove => View.CollectionChanged -= value;
        }

        public void Refresh() => View.Refresh();

        public IEnumerator GetEnumerator() => ((IEnumerable)View).GetEnumerator();

        public IDataGridCollectionView CreateView() => View;

        // Read-only IList over the view's current (filtered, sorted) rows.
        public int Count => View.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public object? this[int index]
        {
            get => View[index];
            set => throw new NotSupportedException();
        }

        public bool Contains(object? value) => View.Contains(value);

        public int IndexOf(object? value) => View.IndexOf(value);

        public void CopyTo(Array array, int index)
        {
            foreach (object? item in this)
            {
                array.SetValue(item, index++);
            }
        }

        public int Add(object? value) => throw new NotSupportedException();

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();
    }

    /// <summary>
    /// Sorted by the given properties and kept sorted as rows change. Avalonia's view has no live sorting, so this
    /// watches the rows' sort properties and, once per UI-thread batch of changes, re-sorts — but only when the
    /// current order is actually broken, so frequent value changes that don't reorder (a per-second countdown) don't
    /// reset the list every tick.
    /// </summary>
    private sealed class LiveSortedItemsView : AvaloniaItemsView
    {
        private readonly IReadOnlyList<ItemsViewSort> _sorts;
        private readonly HashSet<string> _sortProperties;
        private bool _checkQueued;

        public LiveSortedItemsView(IList source, IReadOnlyList<ItemsViewSort> sorts)
            : base(new DataGridCollectionView(source))
        {
            _sorts = sorts;
            _sortProperties = sorts.Select(sort => sort.PropertyName).ToHashSet(StringComparer.Ordinal);
            foreach (ItemsViewSort sort in sorts)
            {
                View.SortDescriptions.Add(DataGridSortDescription.FromPath(
                    sort.PropertyName,
                    sort.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
            }

            foreach (object? item in source)
            {
                Watch(item);
            }

            if (source is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += OnSourceCollectionChanged;
            }
        }

        private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (object? item in e.OldItems ?? Array.Empty<object>())
            {
                Unwatch(item);
            }

            foreach (object? item in e.NewItems ?? Array.Empty<object>())
            {
                Watch(item);
            }
        }

        private void Watch(object? item)
        {
            if (item is INotifyPropertyChanged notifying)
            {
                notifying.PropertyChanged += OnItemPropertyChanged;
            }
        }

        private void Unwatch(object? item)
        {
            if (item is INotifyPropertyChanged notifying)
            {
                notifying.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_checkQueued || e.PropertyName is null || !_sortProperties.Contains(e.PropertyName))
            {
                return;
            }

            _checkQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _checkQueued = false;
                if (!IsInOrder())
                {
                    View.Refresh();
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>Whether the view's current sequence still satisfies the sort (adjacent pairs in order).</summary>
        private bool IsInOrder()
        {
            object? previous = null;
            foreach (object item in (IEnumerable)View)
            {
                if (previous is not null && Compare(previous, item) > 0)
                {
                    return false;
                }

                previous = item;
            }

            return true;
        }

        private int Compare(object left, object right)
        {
            foreach (ItemsViewSort sort in _sorts)
            {
                int result = Comparer.Default.Compare(Read(left, sort.PropertyName), Read(right, sort.PropertyName));
                if (result != 0)
                {
                    return sort.Descending ? -result : result;
                }
            }

            return 0;
        }

        private static object? Read(object item, string propertyName) =>
            item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(item);
    }
}
