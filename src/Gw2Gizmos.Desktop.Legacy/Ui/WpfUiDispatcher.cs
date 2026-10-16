using System;
using System.Windows;
using System.Windows.Threading;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Legacy.Ui;

/// <summary>
/// <see cref="IUiDispatcher"/> over the WPF application's dispatcher, resolved per call so it can be created before
/// the <see cref="Application"/> exists (the log store is built ahead of the host); until then work runs inline.
/// </summary>
public sealed class WpfUiDispatcher : IUiDispatcher
{
    private static Dispatcher? Dispatcher => Application.Current?.Dispatcher;

    public bool CheckAccess() => Dispatcher?.CheckAccess() ?? true;

    public void Post(Action action)
    {
        if (Dispatcher is { } dispatcher)
        {
            dispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    public void Invoke(Action action)
    {
        if (Dispatcher is { } dispatcher)
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public IUiTimer CreateTimer(TimeSpan interval, Action tick) => new WpfUiTimer(interval, tick);

    /// <summary>A <see cref="DispatcherTimer"/> on the creating (UI) thread's dispatcher.</summary>
    private sealed class WpfUiTimer : IUiTimer
    {
        private readonly DispatcherTimer _timer;

        public WpfUiTimer(TimeSpan interval, Action tick)
        {
            _timer = new DispatcherTimer { Interval = interval };
            _timer.Tick += (_, _) => tick();
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();
    }
}
