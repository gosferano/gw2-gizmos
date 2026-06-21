using Gw2Gizmos.Desktop.Controls;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Carries the drilled-into play session (and segment) across the parameterless WPF-UI navigation — the Sessions
/// hub → a session's segment timeline → a segment's loot — the way <see cref="SelectedCharacterService"/> carries
/// the character. Transient navigation context for the current account, so it isn't persisted.
/// </summary>
public sealed class SelectedSessionService
{
    public long? GameSessionId { get; private set; }

    public string GameSessionTitle { get; private set; } = "";

    public long? SegmentId { get; private set; }

    public string SegmentTitle { get; private set; } = "";

    /// <summary>Whether the drilled-into segment is the active (in-progress) one, which can't be deleted.</summary>
    public bool SegmentIsActive { get; private set; }

    public void SelectSession(long id, string title)
    {
        GameSessionId = id;
        GameSessionTitle = title;
    }

    public void SelectSegment(long id, string title, bool isActive)
    {
        SegmentId = id;
        SegmentTitle = title;
        SegmentIsActive = isActive;
    }
}

/// <summary>Builds the Sessions breadcrumb trails: <c>Sessions › ‹when›</c> and <c>Sessions › ‹when› › ‹character›</c>.</summary>
public static class SessionBreadcrumbs
{
    public static BreadcrumbEntry[] Session(string sessionTitle) => new[]
    {
        new BreadcrumbEntry { Title = "Sessions", Target = typeof(SessionsPage) },
        new BreadcrumbEntry { Title = sessionTitle },
    };

    public static BreadcrumbEntry[] Segment(string sessionTitle, string segmentTitle) => new[]
    {
        new BreadcrumbEntry { Title = "Sessions", Target = typeof(SessionsPage) },
        new BreadcrumbEntry { Title = sessionTitle, Target = typeof(SessionPage) },
        new BreadcrumbEntry { Title = segmentTitle },
    };
}
