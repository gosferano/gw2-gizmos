using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gw2Gizmos.Desktop.Navigation;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Makes a control open its item on the Items page when clicked, if that page lists the item: set
/// <c>c:ItemLink.ItemId="{Binding ItemId}"</c> (e.g. on an inventory slot; item names use <see cref="ItemName"/>,
/// which sets it). A linked control gets the <c>item-link</c> style class (hand cursor, and an underline on hover for
/// text, in Themes/Styles.axaml); 0 means "not an item".
/// </summary>
public static class ItemLink
{
    public static readonly AttachedProperty<int> ItemIdProperty =
        AvaloniaProperty.RegisterAttached<InputElement, int>("ItemId", typeof(ItemLink));

    private const string LinkClass = "item-link";

    static ItemLink()
    {
        ItemIdProperty.Changed.AddClassHandler<InputElement>((element, args) =>
        {
            if (args.GetOldValue<int>() <= 0 && args.GetNewValue<int>() > 0)
            {
                element.PointerPressed += OnPointerPressed;
                element.PointerReleased += OnPointerReleased;
            }
            else if (args.GetOldValue<int>() > 0 && args.GetNewValue<int>() <= 0)
            {
                element.PointerPressed -= OnPointerPressed;
                element.PointerReleased -= OnPointerReleased;
            }

            _ = ApplyAsync(element);
        });
    }

    public static int GetItemId(InputElement element) => element.GetValue(ItemIdProperty);

    public static void SetItemId(InputElement element, int value) => element.SetValue(ItemIdProperty, value);

    private static async Task ApplyAsync(InputElement element)
    {
        int itemId = GetItemId(element);
        if (AppServices.ItemLinks is { } links && itemId > 0)
        {
            await links.Ready;
        }

        // Recycled to another item meanwhile: that one's own change handles it.
        if (GetItemId(element) != itemId)
        {
            return;
        }

        element.Classes.Set(LinkClass, IsLinked(element));
    }

    private static bool IsLinked(InputElement element) =>
        GetItemId(element) is > 0 and var itemId && AppServices.ItemLinks?.IsListed(itemId) == true;

    // Claims the press so the row or tree item under the link doesn't also select or expand.
    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is InputElement element && IsLinked(element)
            && e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
        }
    }

    // Opens on release over the link, like a button (pressing and dragging off it cancels).
    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is InputElement element && IsLinked(element) && e.InitialPressMouseButton == MouseButton.Left
            && new Rect(element.Bounds.Size).Contains(e.GetPosition(element)))
        {
            e.Handled = true;
            AppServices.ItemLinks!.Open(GetItemId(element));
        }
    }
}
