using Avalonia;
using Avalonia.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// An item's name, the same everywhere it's shown: coloured by the item's rarity (<see cref="ItemRarity"/>) and, when
/// the Items page lists the item, a link that opens it there (<see cref="ItemLink"/>), shown as one only on hover.
/// Styled as a <see cref="TextBlock"/>, so text styles apply to it as to any other.
/// </summary>
public class ItemName : TextBlock
{
    public static readonly StyledProperty<int> ItemIdProperty = AvaloniaProperty.Register<ItemName, int>(
        nameof(ItemId)
    );

    static ItemName()
    {
        ItemIdProperty.Changed.AddClassHandler<ItemName>((name, args) =>
        {
            ItemRarity.SetItemId(name, name.ItemId);
            ItemLink.SetItemId(name, name.ItemId);
        });
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <summary>The item; 0 for none (plain text).</summary>
    public int ItemId
    {
        get => GetValue(ItemIdProperty);
        set => SetValue(ItemIdProperty, value);
    }
}
