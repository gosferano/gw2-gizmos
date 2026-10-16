using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Legacy.Ui;

/// <summary>
/// Backs <see cref="IItemsView"/> with WPF's <see cref="ListCollectionView"/>, so a bound <c>DataGrid</c> still sorts
/// by column header on top of the view-model's filter.
/// </summary>
public sealed class WpfItemsViewFactory : IItemsViewFactory
{
    public IItemsView Create(IList source) => new WpfItemsView(source);

    public IItemsView CreateLiveSorted(IList source, IReadOnlyList<ItemsViewSort> sorts)
    {
        var view = new WpfItemsView(source) { IsLiveSorting = true };
        foreach (ItemsViewSort sort in sorts)
        {
            view.LiveSortingProperties.Add(sort.PropertyName);
            view.SortDescriptions.Add(new SortDescription(
                sort.PropertyName,
                sort.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }

        return view;
    }

    /// <summary>A <see cref="ListCollectionView"/> already satisfies the seam (Filter, Refresh, enumeration).</summary>
    private sealed class WpfItemsView : ListCollectionView, IItemsView
    {
        public WpfItemsView(IList source)
            : base(source)
        {
        }
    }
}
