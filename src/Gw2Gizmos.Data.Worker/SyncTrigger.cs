namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// A one-shot wake signal for a single sync loop. The loop waits on <see cref="WaitForNextRunAsync"/> (its timer's
/// next tick, or an early wake); the trigger watcher calls <see cref="Signal"/> when the desktop asks for an
/// immediate run. A signal raised while the loop is mid-cycle (not waiting) is remembered and consumed on the next
/// wait, so a trigger is never lost. Coalescing: several signals before the loop next waits collapse into one run.
/// </summary>
public sealed class SyncTrigger
{
    private readonly object _gate = new();
    private CancellationTokenSource? _waiting;
    private bool _pending;

    /// <summary>
    /// Waits until <paramref name="timer"/>'s next tick or until <see cref="Signal"/> fires, whichever is first.
    /// Returns true to run the loop body again; propagates cancellation when <paramref name="stoppingToken"/> is
    /// cancelled (worker shutdown), matching <see cref="PeriodicTimer.WaitForNextTickAsync"/>.
    /// </summary>
    public async Task<bool> WaitForNextRunAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        CancellationTokenSource wake;
        lock (_gate)
        {
            // A trigger arrived while the loop was busy — run now without waiting.
            if (_pending)
            {
                _pending = false;
                return true;
            }

            wake = new CancellationTokenSource();
            _waiting = wake;
        }

        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, wake.Token);
        try
        {
            return await timer.WaitForNextTickAsync(linked.Token);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Woken early by a trigger (not by shutdown) — run the loop body now.
            return true;
        }
        finally
        {
            lock (_gate)
            {
                _waiting = null;
            }

            wake.Dispose();
        }
    }

    /// <summary>Wakes the loop now if it's waiting, otherwise marks a run pending for its next wait.</summary>
    public void Signal()
    {
        lock (_gate)
        {
            if (_waiting is not null)
            {
                _waiting.Cancel();
            }
            else
            {
                _pending = true;
            }
        }
    }
}
