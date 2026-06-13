using System;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// One crumb in a <see cref="PageShell"/> breadcrumb trail. <see cref="Target"/> is the page to navigate to
/// when the crumb is clicked; the current (last) crumb leaves it null.
/// </summary>
public sealed class BreadcrumbEntry
{
    public string Title { get; set; } = "";

    public Type? Target { get; set; }

    public override string ToString() => Title;
}
