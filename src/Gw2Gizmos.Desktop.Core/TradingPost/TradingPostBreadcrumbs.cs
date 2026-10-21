using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>The breadcrumbs of a Trading post section: "Trading post › Orders" and so on.</summary>
public static class TradingPostBreadcrumbs
{
    public static BreadcrumbEntry[] Section(string sectionTitle) =>
    [
        new BreadcrumbEntry { Title = "Trading post", Target = AppPage.TradingPost },
        new BreadcrumbEntry { Title = sectionTitle },
    ];
}
