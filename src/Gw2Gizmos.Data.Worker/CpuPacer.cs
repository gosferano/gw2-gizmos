namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Keeps a long CPU-bound job (the craft-cost runs) to about half a core: called between steps, it pauses after each
/// slice of work for as long as the slice took. The job takes about twice as long, but a core isn't held at full load
/// (and turbo boost) for seconds on end, so it doesn't run hot. The pause blocks the job's thread rather than
/// awaiting: giving the thread back made the job wait for one after every pause while the worker's other syncs kept
/// the thread pool busy (at startup, a run took five times as long). Not thread-safe: one per job.
/// </summary>
public sealed class CpuPacer
{
    /// <summary>How much work runs between pauses.</summary>
    public static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(50);

    private readonly TimeProvider _time;
    private readonly Action<TimeSpan, CancellationToken> _sleep;
    private long _sliceStart;

    /// <param name="sleep">How to wait; tests pass one that doesn't.</param>
    public CpuPacer(TimeProvider time, Action<TimeSpan, CancellationToken>? sleep = null)
    {
        _time = time;
        _sleep = sleep ?? ((wait, cancel) => cancel.WaitHandle.WaitOne(wait));
        _sliceStart = time.GetTimestamp();
    }

    /// <summary>Pauses as long as the work since the last pause took, once that's a whole slice.</summary>
    public void PauseIfDue(CancellationToken cancellationToken)
    {
        TimeSpan worked = _time.GetElapsedTime(_sliceStart);
        if (worked < Slice)
        {
            return;
        }

        _sleep(worked, cancellationToken);
        _sliceStart = _time.GetTimestamp();
    }
}
