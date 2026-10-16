namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>
/// One crumb in a page's breadcrumb trail. <see cref="Target"/> is the page to navigate to when the crumb is
/// clicked; the current (last) crumb leaves it null.
/// </summary>
public sealed class BreadcrumbEntry
{
    public string Title { get; set; } = "";

    public AppPage? Target { get; set; }

    public override string ToString() => Title;
}
