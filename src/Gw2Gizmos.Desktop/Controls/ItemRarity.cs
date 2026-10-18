using Avalonia;
using Avalonia.Controls;
using Gw2Gizmos.Desktop.Navigation;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Colours a text by its item's rarity, like the game: set <c>c:ItemRarity.ItemId="{Binding ItemId}"</c> on an item
/// name's <see cref="TextBlock"/> and it gets a <c>rarity-…</c> style class (e.g. <c>rarity-exotic</c>), coloured in
/// Themes/Styles.axaml for light and dark. Basic and unknown items keep the normal text colour; 0 means "not an item".
/// </summary>
public static class ItemRarity
{
    public static readonly AttachedProperty<int> ItemIdProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, int>("ItemId", typeof(ItemRarity));

    static ItemRarity()
    {
        ItemIdProperty.Changed.AddClassHandler<TextBlock>((text, args) => _ = ApplyAsync(text));
    }

    public static int GetItemId(TextBlock text) => text.GetValue(ItemIdProperty);

    public static void SetItemId(TextBlock text, int value) => text.SetValue(ItemIdProperty, value);

    /// <summary>The style class for a rarity, e.g. "rarity-exotic"; null for Basic or unknown (no colour).</summary>
    public static string? ClassFor(string? rarity) =>
        rarity is null or "Basic" ? null : "rarity-" + rarity.ToLowerInvariant();

    private static async Task ApplyAsync(TextBlock text)
    {
        int itemId = GetItemId(text);
        if (AppServices.Rarities is { } rarities && itemId > 0)
        {
            await rarities.Ready;
        }

        // Recycled to another item meanwhile: that one's own change handles it.
        if (GetItemId(text) != itemId)
        {
            return;
        }

        text.Classes.RemoveAll(text.Classes.Where(c => c.StartsWith("rarity-", StringComparison.Ordinal)).ToList());
        if (ClassFor(itemId > 0 ? AppServices.Rarities?.Of(itemId) : null) is { } rarityClass)
        {
            text.Classes.Add(rarityClass);
        }
    }
}
