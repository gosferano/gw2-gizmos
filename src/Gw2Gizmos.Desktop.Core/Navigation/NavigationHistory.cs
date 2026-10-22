using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>
/// A page visited, with what it showed: pages open on context set before navigating (the selected character, session
/// and segment, an item or event search to open on), so going back has to restore that too.
/// </summary>
public sealed record NavigationEntry(
    AppPage Page,
    string? Character,
    (long Id, string Title)? Session,
    (long Id, string Title, bool IsActive)? Segment,
    int? Item,
    string? EventSearch);

/// <summary>
/// The pages visited, for going back and forward like a browser: each navigation is recorded with its context, and a
/// new one after going back drops what was ahead. Going back or forward restores the entry's context, for the shell to
/// reopen its page.
/// </summary>
public sealed class NavigationHistory
{
    // Enough to go back through a long session without growing forever.
    private const int MaxEntries = 100;

    private readonly SelectedCharacterService _character;
    private readonly SelectedSessionService _session;
    private readonly PageTargets _targets;
    private readonly List<NavigationEntry> _entries = [];
    private int _index = -1;

    public NavigationHistory(SelectedCharacterService character, SelectedSessionService session, PageTargets targets)
    {
        _character = character;
        _session = session;
        _targets = targets;
    }

    /// <summary>Raised when <see cref="CanGoBack"/> or <see cref="CanGoForward"/> may have changed.</summary>
    public event EventHandler? Changed;

    public bool CanGoBack => _index > 0;

    public bool CanGoForward => _index < _entries.Count - 1;

    /// <summary>
    /// Records a navigation to <paramref name="page"/>, as it opens: with the context it'll read, and its target before
    /// it takes it. Opening the page just shown, on the same context, isn't a new entry.
    /// </summary>
    public void Record(AppPage page)
    {
        NavigationEntry entry = Capture(page);
        if (_index >= 0 && _entries[_index] == entry)
        {
            return;
        }

        _entries.RemoveRange(_index + 1, _entries.Count - _index - 1);
        _entries.Add(entry);
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(0);
        }

        _index = _entries.Count - 1;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Steps back, restoring that entry's context; returns its page, or null at the start.</summary>
    public AppPage? Back() => CanGoBack ? Move(-1) : null;

    /// <summary>Steps forward, restoring that entry's context; returns its page, or null at the end.</summary>
    public AppPage? Forward() => CanGoForward ? Move(1) : null;

    private AppPage Move(int step)
    {
        _index += step;
        NavigationEntry entry = _entries[_index];
        Restore(entry);
        Changed?.Invoke(this, EventArgs.Empty);
        return entry.Page;
    }

    private NavigationEntry Capture(AppPage page) => new(
        page,
        _character.CharacterName,
        _session.GameSessionId is { } sessionId ? (sessionId, _session.GameSessionTitle) : null,
        _session.SegmentId is { } segmentId ? (segmentId, _session.SegmentTitle, _session.SegmentIsActive) : null,
        _targets.PendingItem,
        _targets.PendingEventSearch);

    private void Restore(NavigationEntry entry)
    {
        if (entry.Character is { } character)
        {
            _character.Select(character);
        }

        if (entry.Session is var (sessionId, sessionTitle))
        {
            _session.SelectSession(sessionId, sessionTitle);
        }

        if (entry.Segment is var (segmentId, segmentTitle, isActive))
        {
            _session.SelectSegment(segmentId, segmentTitle, isActive);
        }

        if (entry.Item is { } item)
        {
            _targets.OpenItem(item);
        }

        if (entry.EventSearch is { } search)
        {
            _targets.OpenEvents(search);
        }
    }
}
