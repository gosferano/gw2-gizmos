using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Tails the worker process's current rolling Serilog file and merges its lines into the shared
/// <see cref="LogStore"/> (tagged <c>Source = Worker</c>), so the in-app viewer shows both processes.
/// Starts at the current end of the file (no history replay) and follows daily rollover.
/// </summary>
public sealed class WorkerLogTailer : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1.5);
    private static readonly Regex LineRegex = new(
        @"^(\d{2}:\d{2}:\d{2}\.\d{3}) \[([A-Za-z]{3})\] \[[^\]]*\] (.*)$",
        RegexOptions.Compiled
    );

    private readonly LogStore _store;
    private readonly string _logsDir;

    private string? _currentPath;
    private long _offset;
    private DateTimeOffset _lastTimestamp = DateTimeOffset.Now;
    private string _lastLevel = "INF";

    public WorkerLogTailer(LogStore store)
    {
        _store = store;
        _logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gw2Gizmos",
            "Logs"
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start at the tail of the current file so we follow new lines without replaying history.
        _currentPath = GetCurrentLogPath();
        _offset = _currentPath is not null && File.Exists(_currentPath) ? new FileInfo(_currentPath).Length : 0;

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                ReadNew();
            }
            catch
            {
                // Transient IO (rollover, locked write) — retry next tick.
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void ReadNew()
    {
        string? path = GetCurrentLogPath();
        if (path is null)
        {
            return;
        }

        if (!string.Equals(path, _currentPath, StringComparison.OrdinalIgnoreCase))
        {
            _currentPath = path;
            _offset = 0;
        }

        var info = new FileInfo(path);
        if (!info.Exists || info.Length < _offset)
        {
            _offset = 0;
        }

        if (info.Length <= _offset)
        {
            return;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(_offset, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string chunk = reader.ReadToEnd();

        int lastNewline = chunk.LastIndexOf('\n');
        if (lastNewline < 0)
        {
            return; // no complete line yet
        }

        string complete = chunk.Substring(0, lastNewline + 1);
        _offset += Encoding.UTF8.GetByteCount(complete);

        var batch = new List<LogEntry>();
        foreach (string raw in complete.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length != 0)
            {
                batch.Add(ParseLine(line));
            }
        }

        // One UI-thread hop for the whole poll's worth of lines.
        _store.AddRange(batch);
    }

    private LogEntry ParseLine(string line)
    {
        Match match = LineRegex.Match(line);
        if (match.Success)
        {
            _lastLevel = match.Groups[2].Value.ToUpperInvariant();
            if (
                DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "HH:mm:ss.fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsed
                )
            )
            {
                _lastTimestamp = new DateTimeOffset(DateTime.Today.Add(parsed.TimeOfDay));
            }

            return new LogEntry(_lastTimestamp, _lastLevel, "Worker", match.Groups[3].Value);
        }

        // Continuation line (e.g. an exception stack trace) — carry the previous level/time.
        return new LogEntry(_lastTimestamp, _lastLevel, "Worker", line);
    }

    private string? GetCurrentLogPath()
    {
        if (!Directory.Exists(_logsDir))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(_logsDir, "worker-*.txt")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()
            ?.FullName;
    }
}
