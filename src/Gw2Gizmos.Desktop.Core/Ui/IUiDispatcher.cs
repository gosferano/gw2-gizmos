namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>
/// The UI thread, as the view-models see it: marshal work onto it and run timers whose ticks land on it. Each UI
/// head implements it over its own dispatcher; before the UI is up (or with no UI at all) work runs inline.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>True when the caller is already on the UI thread (or there is no UI thread to marshal to).</summary>
    bool CheckAccess();

    /// <summary>Queues <paramref name="action"/> on the UI thread and returns without waiting for it.</summary>
    void Post(Action action);

    /// <summary>Runs <paramref name="action"/> on the UI thread and waits for it to finish.</summary>
    void Invoke(Action action);

    /// <summary>A stopped timer that raises <paramref name="tick"/> on the UI thread every <paramref name="interval"/>.</summary>
    IUiTimer CreateTimer(TimeSpan interval, Action tick);
}

/// <summary>A repeating timer whose ticks run on the UI thread; created stopped.</summary>
public interface IUiTimer
{
    void Start();

    void Stop();
}
