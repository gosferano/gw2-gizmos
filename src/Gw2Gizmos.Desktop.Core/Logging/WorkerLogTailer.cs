using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Tails the worker process's current rolling Serilog file and merges its lines into the shared
/// <see cref="LogStore"/> (tagged <c>Source = Worker</c>), so the in-app viewer shows both processes.
/// Starts at the current end of the file (no history replay) and follows daily rollover. Lines that don't start an
/// entry (an exception's stack trace) are kept with the entry they follow, as its details. The file's times are UTC;
/// entries carry them as local time for display.
/// </summary>
public sealed class WorkerLogTailer : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1.5);
    private static readonly Regex LineRegex = new(
        @"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z) \[([A-Za-z]{3})\] \[[^\]]*\] (.*)$",
        RegexOptions.Compiled
    );

    private readonly LogStore _store;
    private readonly string _logsDir;

    private string? _currentPath;
    private long _offset;
    private DateTimeOffset _lastTimestamp = DateTimeOffset.Now;
    private string _lastLevel = "INF";

    public WorkerLogTailer(LogStore store, AppPaths paths)
    {
        _store = store;
        // Derive from the shared per-user data dir (so a dev build's tailer reads the dev worker's logs, not the
        // release folder); the worker is spawned with this as its working directory and writes Logs/ under it.
        _logsDir = Path.Combine(paths.DataDirectory, "Logs");
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

        // One UI-thread hop for the whole poll's worth of lines.
        _store.AddRange(Parse(complete.Split('\n')));
    }

    /// <summary>
    /// The entries in a run of lines: each line that starts an entry (time, level, source) begins one, and the lines
    /// after it that don't (an exception's stack trace) become its details. Continuation lines at the very start of a
    /// run, whose entry came in an earlier read, form an entry of their own with the previous level and time.
    /// </summary>
    internal List<LogEntry> Parse(IEnumerable<string> lines)
    {
        var entries = new List<LogEntry>();
        string? message = null;
        var details = new List<string>();

        void Flush()
        {
            if (message is not null)
            {
                string? text = details.Count > 0 ? string.Join('\n', details) : null;
                entries.Add(new LogEntry(_lastTimestamp, _lastLevel, "Worker", message, text));
            }

            message = null;
            details.Clear();
        }

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            Match match = LineRegex.Match(line);
            if (!match.Success)
            {
                if (message is null)
                {
                    message = line;
                }
                else
                {
                    details.Add(line);
                }

                continue;
            }

            Flush();
            _lastLevel = match.Groups[2].Value.ToUpperInvariant();
            if (
                DateTimeOffset.TryParseExact(
                    match.Groups[1].Value,
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTimeOffset parsed
                )
            )
            {
                _lastTimestamp = parsed.ToLocalTime();
            }

            message = match.Groups[3].Value;
        }

        Flush();
        return entries;
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
