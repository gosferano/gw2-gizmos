namespace Gw2Gizmos.Desktop.Core.Platform;

/// <summary>
/// The OS accent colour, for desktops where the UI toolkit can't read it itself (e.g. GNOME-style settings on a Linux
/// desktop the toolkit doesn't recognise). Where the toolkit follows the OS accent on its own (Windows, KDE) this
/// reports nothing and the toolkit's value stands.
/// </summary>
public interface ISystemAccentColor
{
    /// <summary>The accent as 0xAARRGGBB, or null to leave the accent to the UI toolkit.</summary>
    uint? Current { get; }

    /// <summary>Raised, on any thread, when <see cref="Current"/> changes.</summary>
    event Action? Changed;
}

/// <summary><see cref="ISystemAccentColor"/> where the UI toolkit reads the OS accent itself (Windows).</summary>
public sealed class ToolkitResolvedAccentColor : ISystemAccentColor
{
    public uint? Current => null;

    public event Action? Changed
    {
        add { }
        remove { }
    }
}
