namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>
/// What a page should open on, set just before navigating to it (e.g. by the global search): the Items page selects
/// an item, the Events page filters to an event. Each target is taken once, by the next page to open, so a later
/// visit opens as usual.
/// </summary>
public sealed class PageTargets
{
    private int? _itemId;
    private string? _eventSearch;

    /// <summary>The Items page opens with this item selected (and its name as the filter).</summary>
    public void OpenItem(int itemId) => _itemId = itemId;

    /// <summary>The Events page opens filtered by this text (an event's name).</summary>
    public void OpenEvents(string search) => _eventSearch = search;

    public int? TakeItem()
    {
        int? itemId = _itemId;
        _itemId = null;
        return itemId;
    }

    public string? TakeEventSearch()
    {
        string? search = _eventSearch;
        _eventSearch = null;
        return search;
    }
}
