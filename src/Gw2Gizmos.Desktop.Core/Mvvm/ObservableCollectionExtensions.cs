using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core.Mvvm;

public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Makes the collection hold <paramref name="items"/>, in that order, changing only what differs: items equal to
    /// ones already there are kept (moved into place if needed), and the rest are replaced, inserted or removed. A live
    /// refresh that changed nothing then changes nothing on screen — the list keeps its controls, so expanded sections,
    /// scroll position, hover and loaded icons stay as they were — and one that did change only touches those rows.
    /// Rows are matched by value, so they should be records (or otherwise compare by value).
    /// </summary>
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items) =>
        Reconcile(collection, items.ToList(), EqualityComparer<T>.Default.Equals, update: null);

    /// <summary>
    /// As <see cref="ReplaceWith{T}(ObservableCollection{T}, IEnumerable{T})"/>, but for rows that update themselves:
    /// rows are matched by <paramref name="key"/>, and a row already there is kept and handed its new version via
    /// <paramref name="update"/> instead of being replaced — e.g. a category that refreshes its own cells, so its
    /// expander stays open.
    /// </summary>
    public static void ReplaceWith<T, TKey>(
        this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, TKey> key, Action<T, T> update) =>
        Reconcile(collection, items.ToList(), (a, b) => EqualityComparer<TKey>.Default.Equals(key(a), key(b)), update);

    private static void Reconcile<T>(
        ObservableCollection<T> collection, List<T> next, Func<T, T, bool> same, Action<T, T>? update)
    {
        for (int i = 0; i < next.Count; i++)
        {
            T incoming = next[i];

            // Already there (here or further on): keep it, moved into place.
            int existing = IndexOf(collection, incoming, same, from: i);
            if (existing >= 0)
            {
                if (existing != i)
                {
                    collection.Move(existing, i);
                }

                update?.Invoke(collection[i], incoming);
                continue;
            }

            // New here: take the place of a row that isn't needed later, otherwise slot in before it.
            if (i < collection.Count && IndexOf(next, collection[i], same, from: i + 1) < 0)
            {
                collection[i] = incoming;
            }
            else
            {
                collection.Insert(i, incoming);
            }
        }

        while (collection.Count > next.Count)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }

    private static int IndexOf<T>(IList<T> list, T item, Func<T, T, bool> same, int from)
    {
        for (int i = from; i < list.Count; i++)
        {
            if (same(list[i], item))
            {
                return i;
            }
        }

        return -1;
    }
}
