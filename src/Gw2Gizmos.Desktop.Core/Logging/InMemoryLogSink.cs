using Serilog.Core;
using Serilog.Events;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Serilog sink that pushes the desktop app's own log events into the in-app <see cref="LogStore"/>, an
/// event's exception (or any further lines of its message) as the entry's details.</summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly LogStore _store;

    public InMemoryLogSink(LogStore store)
    {
        _store = store;
    }

    public void Emit(LogEvent logEvent)
    {
        string text = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
        {
            text = $"{text}\n{logEvent.Exception}";
        }

        string[] lines = text.ReplaceLineEndings("\n").Split('\n', 2);
        string? details = lines.Length > 1 ? lines[1] : null;
        _store.Add(new LogEntry(logEvent.Timestamp, Abbreviate(logEvent.Level), "Desktop", lines[0], details));
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
