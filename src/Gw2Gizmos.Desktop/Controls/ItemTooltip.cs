using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Navigation;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Gives a control its item's hover text: set <c>c:ItemTooltip.ItemId="{Binding ItemId}"</c> (item icons and names,
/// <see cref="ItemImage"/> and <see cref="ItemName"/>, set it themselves). The text is the item's name in its rarity
/// colour, then its buy order, listing and what it's worth (<see cref="ItemTooltipView"/>), looked up when it first
/// opens. 0 means "not an item" (no hover text).
/// </summary>
public static class ItemTooltip
{
    public static readonly AttachedProperty<int> ItemIdProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("ItemId", typeof(ItemTooltip));

    static ItemTooltip()
    {
        ItemIdProperty.Changed.AddClassHandler<Control>((control, args) =>
            ToolTip.SetTip(
                control, args.GetNewValue<int>() is > 0 and var itemId ? new ItemTooltipView(itemId) : null));
    }

    public static int GetItemId(Control control) => control.GetValue(ItemIdProperty);

    public static void SetItemId(Control control, int value) => control.SetValue(ItemIdProperty, value);
}

/// <summary>
/// An item's hover text: its name in its rarity colour, then the highest buy order and lowest listing when it trades,
/// and what gizmos counts it as worth, with where that comes from. Prices load when it's first shown.
/// </summary>
public sealed class ItemTooltipView : StackPanel
{
    private readonly int _itemId;
    private bool _loaded;

    public ItemTooltipView(int itemId)
    {
        _itemId = itemId;
        Spacing = 4;
        MinWidth = 180;
        Children.Add(new TextBlock { Text = "Loading…", Opacity = 0.7 });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_loaded)
        {
            _loaded = true;
            _ = LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (AppServices.ItemPrices is not { } prices)
        {
            return;
        }

        ItemPrice? price = await prices.GetAsync(_itemId);
        Children.Clear();
        var name = new TextBlock
        {
            Text = price?.Name ?? $"Item {_itemId}",
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 320,
        };
        ItemRarity.SetItemId(name, _itemId);
        Children.Add(name);
        if (price is null)
        {
            return;
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), RowSpacing = 2 };
        if (price.Buy is { } buy)
        {
            AddRow(grid, "Buy order", buy);
        }

        if (price.Sell is { } sell)
        {
            AddRow(grid, "Listing", sell);
        }

        if (price.Source != ItemValueSource.None)
        {
            AddRow(grid, "Worth", price.Value, price.Source switch
            {
                ItemValueSource.InstantSell => "selling instantly, after fees",
                ItemValueSource.CoinCost => "what it costs for coin",
                _ => "from a vendor",
            });
        }
        else if (price.Buy is null && price.Sell is null)
        {
            Children.Add(new TextBlock { Text = "No value", Opacity = 0.7 });
        }

        if (grid.Children.Count > 0)
        {
            Children.Add(grid);
        }
    }

    // A labelled coin amount, with a note under it.
    private static void AddRow(Grid grid, string label, long copper, string? note = null)
    {
        int row = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var text = new TextBlock { Text = label, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(text, row);
        grid.Children.Add(text);

        var amount = new StackPanel();
        amount.Children.Add(new CoinDisplay { Copper = copper, HorizontalAlignment = HorizontalAlignment.Left });
        if (note is not null)
        {
            amount.Children.Add(new TextBlock { Text = note, FontSize = 11, Opacity = 0.7 });
        }

        Grid.SetRow(amount, row);
        Grid.SetColumn(amount, 2);
        grid.Children.Add(amount);
    }
}
