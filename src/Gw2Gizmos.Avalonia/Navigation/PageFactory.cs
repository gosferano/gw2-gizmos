using Avalonia.Controls;
using Avalonia.Layout;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Avalonia.Navigation;

/// <summary>
/// Creates the view for each <see cref="AppPage"/>. Pages are ported one at a time; until a page exists it shows a
/// placeholder naming it.
/// </summary>
public sealed class PageFactory
{
    public Control Create(AppPage page) => new TextBlock
    {
        Text = $"{page} — not ported to Avalonia yet.",
        Opacity = 0.6,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };
}
