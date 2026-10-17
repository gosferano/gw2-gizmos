using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Refreshing a list changes only what differs, so unchanged rows (and their controls) stay put.</summary>
public sealed class ObservableCollectionExtensionsTests
{
    private sealed record Row(int Id, string Name);

    private sealed class Category(string name, int count)
    {
        public string Name { get; } = name;

        public int Count { get; set; } = count;
    }

    private static (ObservableCollection<T> Collection, List<NotifyCollectionChangedAction> Changes) Watch<T>(params T[] items)
    {
        var collection = new ObservableCollection<T>(items);
        var changes = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => changes.Add(e.Action);
        return (collection, changes);
    }

    [Fact]
    public void Unchanged_items_raise_no_change()
    {
        (ObservableCollection<Row> rows, List<NotifyCollectionChangedAction> changes) = Watch(new Row(1, "a"), new Row(2, "b"));

        rows.ReplaceWith(new[] { new Row(1, "a"), new Row(2, "b") });

        Assert.Empty(changes);
    }

    [Fact]
    public void Only_a_changed_item_is_replaced()
    {
        var kept = new Row(1, "a");
        (ObservableCollection<Row> rows, List<NotifyCollectionChangedAction> changes) = Watch(kept, new Row(2, "b"), new Row(3, "c"));

        rows.ReplaceWith(new[] { new Row(1, "a"), new Row(2, "B"), new Row(3, "c") });

        Assert.Equal(new[] { new Row(1, "a"), new Row(2, "B"), new Row(3, "c") }, rows);
        Assert.Same(kept, rows[0]);
        Assert.Equal(new[] { NotifyCollectionChangedAction.Replace }, changes);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 3 })]          // removed from the middle
    [InlineData(new[] { 1, 3 }, new[] { 1, 2, 3 })]          // inserted in the middle
    [InlineData(new[] { 1, 2, 3 }, new[] { 3, 1, 2 })]       // reordered
    [InlineData(new[] { 1, 2, 3 }, new int[0])]              // emptied
    [InlineData(new int[0], new[] { 4, 5 })]                 // filled
    [InlineData(new[] { 0, 0, 7, 0 }, new[] { 0, 7, 0, 0 })] // duplicates (e.g. empty slots)
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 4, 9, 2 })]    // a mix
    public void Ends_up_holding_exactly_the_new_items(int[] before, int[] after)
    {
        var collection = new ObservableCollection<int>(before);

        collection.ReplaceWith(after);

        Assert.Equal(after, collection);
    }

    [Fact]
    public void Keyed_rows_are_kept_and_updated_in_place()
    {
        var fine = new Category("Fine", 1);
        (ObservableCollection<Category> categories, List<NotifyCollectionChangedAction> changes) = Watch(fine, new Category("Basic", 2));

        categories.ReplaceWith(
            new[] { new Category("Fine", 5), new Category("Basic", 2), new Category("Gems", 3) },
            c => c.Name,
            (existing, incoming) => existing.Count = incoming.Count);

        Assert.Same(fine, categories[0]);
        Assert.Equal(5, fine.Count);
        Assert.Equal(new[] { "Fine", "Basic", "Gems" }, categories.Select(c => c.Name));
        Assert.Equal(new[] { NotifyCollectionChangedAction.Add }, changes);
    }
}
