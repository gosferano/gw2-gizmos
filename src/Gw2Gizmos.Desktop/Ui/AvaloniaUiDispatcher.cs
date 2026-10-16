using Avalonia.Threading;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary><see cref="IUiDispatcher"/> over Avalonia's UI-thread <see cref="Dispatcher"/>.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action) => Dispatcher.UIThread.Post(action);

    public void Invoke(Action action) => Dispatcher.UIThread.Invoke(action);

    public IUiTimer CreateTimer(TimeSpan interval, Action tick) => new AvaloniaUiTimer(interval, tick);

    /// <summary>An Avalonia <see cref="DispatcherTimer"/>, ticking on the UI thread.</summary>
    private sealed class AvaloniaUiTimer : IUiTimer
    {
        private readonly DispatcherTimer _timer;

        public AvaloniaUiTimer(TimeSpan interval, Action tick)
        {
            _timer = new DispatcherTimer { Interval = interval };
            _timer.Tick += (_, _) => tick();
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();
    }
}
