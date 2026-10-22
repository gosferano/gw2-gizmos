using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>The breadcrumbs of an Advisor section: "Advisor › Craft from stock" and so on.</summary>
public static class AdvisorBreadcrumbs
{
    public static BreadcrumbEntry[] Section(string sectionTitle) =>
    [
        new BreadcrumbEntry { Title = "Advisor", Target = AppPage.Advisor },
        new BreadcrumbEntry { Title = sectionTitle },
    ];
}
