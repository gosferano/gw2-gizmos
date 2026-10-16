namespace Gw2Gizmos.Desktop.Core;

/// <summary>How a map is named in the app: its API name, plus the team colour for WvW borderlands.</summary>
internal static class MapLabel
{
    /// <summary>
    /// E.g. "Dragon's End", or "Alpine Borderlands (Green)". Green's and Blue's borderlands share the name "Alpine
    /// Borderlands" and are separate maps, so the colour (from the map type) tells them apart.
    /// </summary>
    public static string For(string name, string mapType) => mapType switch
    {
        "GreenHome" => $"{name} (Green)",
        "BlueHome" => $"{name} (Blue)",
        "RedHome" => $"{name} (Red)",
        _ => name,
    };
}
