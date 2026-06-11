using Serilog.Core;
using Serilog.Events;

namespace Gw2Gizmos.Desktop;

/// <summary>Serilog sink that pushes the desktop app's own log events into the in-app <see cref="LogStore"/>.</summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly LogStore _store;

    public InMemoryLogSink(LogStore store)
    {
        _store = store;
    }

    public void Emit(LogEvent logEvent)
    {
        _store.Add(new LogEntry(logEvent.Timestamp, Abbreviate(logEvent.Level), "Desktop", logEvent.RenderMessage()));
    }

    private static string Abbreviate(LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => "INF",
        };
}
