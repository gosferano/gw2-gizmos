using System.Collections;

namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>
/// A filterable view over a source collection that list and grid controls bind to directly (their
/// <c>ItemsSource</c>). Each UI head backs it with its native collection view, so a grid's own column-header
/// sorting keeps working on top of the view-model's filter.
/// </summary>
public interface IItemsView : IEnumerable
{
    /// <summary>Rows for which this returns false are hidden; null shows every row.</summary>
    Predicate<object>? Filter { get; set; }

    /// <summary>Re-applies <see cref="Filter"/> (and any sort) after the state it reads has changed.</summary>
    void Refresh();
}

/// <summary>One sort criterion for <see cref="IItemsViewFactory.CreateLiveSorted"/>: a row property and direction.</summary>
public readonly record struct ItemsViewSort(string PropertyName, bool Descending = false);

/// <summary>Creates <see cref="IItemsView"/>s; implemented by each UI head.</summary>
public interface IItemsViewFactory
{
    /// <summary>A view over <paramref name="source"/> (an <see cref="IList"/>, typically observable).</summary>
    IItemsView Create(IList source);

    /// <summary>
    /// A view kept ordered by <paramref name="sorts"/> (first criterion first) that re-sorts a row in place when one
    /// of those properties changes (<see cref="System.ComponentModel.INotifyPropertyChanged"/>), without a reset.
    /// </summary>
    IItemsView CreateLiveSorted(IList source, IReadOnlyList<ItemsViewSort> sorts);
}
