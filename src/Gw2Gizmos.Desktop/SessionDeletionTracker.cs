using System.Collections.Generic;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Remembers the play sessions and character segments the user deleted this app run, so the read-only session views
/// can hide them immediately instead of waiting on the worker. A delete is only queued here on the desktop and runs
/// on the worker a few seconds later (it's the DB's sole writer); without this, navigating back to a list would
/// re-read the still-present row from the DB and the just-deleted item would reappear until the worker caught up.
/// In-memory only: sessions are historical and never re-sync, so a deleted id stays hidden for the rest of the run.
/// </summary>
public sealed class SessionDeletionTracker
{
    private readonly object _gate = new();
    private readonly HashSet<long> _sessions = new();
    private readonly HashSet<long> _segments = new();

    public void MarkSession(long sessionId)
    {
        lock (_gate)
        {
            _sessions.Add(sessionId);
        }
    }

    public void MarkSegment(long segmentId)
    {
        lock (_gate)
        {
            _segments.Add(segmentId);
        }
    }

    public bool IsSessionDeleted(long sessionId)
    {
        lock (_gate)
        {
            return _sessions.Contains(sessionId);
        }
    }

    public bool IsSegmentDeleted(long segmentId)
    {
        lock (_gate)
        {
            return _segments.Contains(segmentId);
        }
    }
}
