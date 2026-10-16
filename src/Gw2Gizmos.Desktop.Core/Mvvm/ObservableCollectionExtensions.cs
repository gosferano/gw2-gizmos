using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gw2Gizmos.Desktop.Core.Mvvm;

public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Replaces the contents with <paramref name="items"/>. Done in one go on the UI thread, the list is laid out once
    /// with the new items, so a live refresh doesn't flash empty or lose its scroll position.
    /// </summary>
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (T item in items)
        {
            collection.Add(item);
        }
    }
}
