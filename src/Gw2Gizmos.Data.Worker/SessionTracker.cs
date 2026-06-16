using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.MumbleLink.Client;
using Gw2Gizmos.MumbleLink.Contract;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Detects play sessions from MumbleLink and records them as <see cref="GameSession"/> + ordered
/// <see cref="CharacterSegment"/> rows. The game process (<c>Context.ProcessId</c>) being alive drives the
/// game-session boundaries (launch → close), so a long alt-tab/minimise keeps the session open; the <c>uiTick</c>
/// freeze is only a fallback for when the process can't be read. The identity's active character drives the
/// segment boundaries (the switches within a sitting).
/// At every boundary it runs an account sync first, then stamps the boundary time, so the observation logs carry a
/// fresh data point just before each timestamp — the desktop reconstructs per-segment "hoarded" deltas from those.
/// Gated on <see cref="WorkerFeatures.PlaySessions"/>; only registered on Windows (the reader uses Windows named
/// shared memory).
/// </summary>
public sealed class SessionTracker : BackgroundService
{
    // Poll the (cheap, in-memory) shared block once a second. Game-end is normally detected from the game process
    // exiting; the tick-freeze timeout is only a fallback for when the process id can't be read.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FreezeTimeout = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMumbleLinkReader _reader;
    private readonly IFeatureGate _featureGate;
    private readonly AccountSyncGate _accountSyncGate;
    private readonly ILogger<SessionTracker> _logger;

    // In-memory state for the currently-open session/segment.
    private long? _gameSessionId;
    private long? _segmentId;
    private string? _activeCharacter;
    private int _nextSequence;
    private uint _lastTick;
    private DateTimeOffset _lastTickChangeUtc;

    public SessionTracker(
        IServiceScopeFactory scopeFactory,
        IMumbleLinkReader reader,
        IFeatureGate featureGate,
        AccountSyncGate accountSyncGate,
        ILogger<SessionTracker> logger
    )
    {
        _scopeFactory = scopeFactory;
        _reader = reader;
        _featureGate = featureGate;
        _accountSyncGate = accountSyncGate;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResumeOrCloseOpenSessionsAsync(stoppingToken);

        _lastTickChangeUtc = DateTimeOffset.UtcNow;
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session tracker tick failed; continuing.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        if (!_featureGate.IsEnabled(WorkerFeatures.PlaySessions.Key))
        {
            // Feature turned off mid-session — close cleanly so we don't leave a dangling open session.
            if (_gameSessionId is not null)
            {
                await EndGameSessionAsync(stoppingToken);
            }

            return;
        }

        MumbleLinkSnapshot? snapshot = _reader.Read();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        string? character = null;
        bool? processAlive = null;
        bool tickFrozen = true;
        if (snapshot is not null)
        {
            if (snapshot.UiTick != _lastTick)
            {
                _lastTick = snapshot.UiTick;
                _lastTickChangeUtc = now;
            }

            tickFrozen = now - _lastTickChangeUtc >= FreezeTimeout;
            character = string.IsNullOrEmpty(snapshot.Identity?.Name) ? null : snapshot.Identity!.Name;
            processAlive = IsGameProcessAlive(snapshot);
        }

        // The game has ended when there's no block, the game process has exited, or — when the process can't be
        // determined — the tick has been frozen past the fallback timeout. A live process keeps the session open
        // through a long alt-tab/minimise (when the tick freezes but the game is still running).
        bool ended = snapshot is null || processAlive == false || (processAlive is null && tickFrozen);

        if (_gameSessionId is not null && ended)
        {
            await EndGameSessionAsync(stoppingToken);
            return;
        }

        if (ended)
        {
            // No live game and nothing open (also the pre-launch "no mapping" state) — nothing to do.
            return;
        }

        if (_gameSessionId is null)
        {
            // Game became live; only open once a character has actually loaded (skip the char-select screen).
            if (character is not null)
            {
                await StartGameSessionAsync(character, stoppingToken);
            }

            return;
        }

        if (character is null)
        {
            // At character-select / loading: close the current character's segment and await the next character.
            if (_segmentId is not null)
            {
                await EndCurrentSegmentAsync(stoppingToken);
            }

            return;
        }

        if (_activeCharacter is null)
        {
            // Returned from char-select to a character — open the next segment.
            await StartSegmentAsync(character, stoppingToken);
            return;
        }

        if (!string.Equals(character, _activeCharacter, StringComparison.Ordinal))
        {
            // A switch with no observed char-select gap — close the old segment and open the new one in one boundary.
            await SwitchSegmentAsync(character, stoppingToken);
        }
    }

    private async Task StartGameSessionAsync(string character, CancellationToken stoppingToken)
    {
        DateTimeOffset boundary = await SyncAndStampAsync(stoppingToken);

        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        string? accountId = await ResolveAccountIdAsync(db, character, stoppingToken);
        if (accountId is null)
        {
            _logger.LogDebug("No account resolved for character {Character}; not starting a session yet.", character);
            return;
        }

        var session = new GameSession { AccountId = accountId, StartedAtUtc = boundary };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(stoppingToken);

        var segment = new CharacterSegment
        {
            GameSessionId = session.Id,
            Sequence = 0,
            CharacterName = character,
            StartedAtUtc = boundary,
        };
        db.CharacterSegments.Add(segment);
        await db.SaveChangesAsync(stoppingToken);

        _gameSessionId = session.Id;
        _segmentId = segment.Id;
        _activeCharacter = character;
        _nextSequence = 1;
        _logger.LogInformation("Play session started for {Account} on {Character}.", accountId, character);
    }

    private async Task StartSegmentAsync(string character, CancellationToken stoppingToken)
    {
        DateTimeOffset boundary = await SyncAndStampAsync(stoppingToken);

        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        var segment = new CharacterSegment
        {
            GameSessionId = _gameSessionId!.Value,
            Sequence = _nextSequence++,
            CharacterName = character,
            StartedAtUtc = boundary,
        };
        db.CharacterSegments.Add(segment);
        await db.SaveChangesAsync(stoppingToken);

        _segmentId = segment.Id;
        _activeCharacter = character;
        _logger.LogInformation("Character segment started on {Character}.", character);
    }

    private async Task EndCurrentSegmentAsync(CancellationToken stoppingToken)
    {
        string? character = _activeCharacter;
        DateTimeOffset boundary = await SyncAndStampAsync(stoppingToken);
        await CloseSegmentAsync(boundary, stoppingToken);
        _logger.LogInformation("Character {Character} left to character select.", character ?? "(unknown)");
    }

    private async Task SwitchSegmentAsync(string character, CancellationToken stoppingToken)
    {
        DateTimeOffset boundary = await SyncAndStampAsync(stoppingToken);

        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        CharacterSegment? previous = await db.CharacterSegments.FirstOrDefaultAsync(s => s.Id == _segmentId, stoppingToken);
        if (previous is { EndedAtUtc: null })
        {
            previous.EndedAtUtc = boundary;
        }

        var segment = new CharacterSegment
        {
            GameSessionId = _gameSessionId!.Value,
            Sequence = _nextSequence++,
            CharacterName = character,
            StartedAtUtc = boundary,
        };
        db.CharacterSegments.Add(segment);
        await db.SaveChangesAsync(stoppingToken);

        _segmentId = segment.Id;
        _activeCharacter = character;
        _logger.LogInformation("Character switched to {Character}.", character);
    }

    private async Task EndGameSessionAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset boundary = await SyncAndStampAsync(stoppingToken);

        if (_segmentId is not null)
        {
            await CloseSegmentAsync(boundary, stoppingToken);
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        GameSession? session = await db.GameSessions.FirstOrDefaultAsync(s => s.Id == _gameSessionId, stoppingToken);
        if (session is { EndedAtUtc: null })
        {
            session.EndedAtUtc = boundary;
            await db.SaveChangesAsync(stoppingToken);
        }

        _logger.LogInformation("Play session ended for {Account}.", session?.AccountId ?? "(unknown)");
        ResetState();
    }

    private async Task CloseSegmentAsync(DateTimeOffset boundary, CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        CharacterSegment? segment = await db.CharacterSegments.FirstOrDefaultAsync(s => s.Id == _segmentId, stoppingToken);
        if (segment is { EndedAtUtc: null })
        {
            segment.EndedAtUtc = boundary;
            await db.SaveChangesAsync(stoppingToken);
        }

        _segmentId = null;
        _activeCharacter = null;
    }

    /// <summary>
    /// Runs a full account sync (serialized against the periodic loop), then returns the boundary timestamp. The
    /// sync lands fresh observations just before the returned time, so a later "totals as-of this boundary" read
    /// includes them. A sync failure is logged but still yields a timestamp — the boundary is recorded regardless.
    /// </summary>
    private async Task<DateTimeOffset> SyncAndStampAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _accountSyncGate.RunExclusivelyAsync(
                async () =>
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var updater = scope.ServiceProvider.GetRequiredService<AccountSyncUpdater>();
                    await updater.SyncAccount(stoppingToken);
                },
                stoppingToken
            );
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Session-boundary account sync failed; recording the boundary anyway.");
        }

        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Whether the Guild Wars 2 process the block reports (<c>Context.ProcessId</c>) is still running: <c>true</c>
    /// alive, <c>false</c> exited, <c>null</c> when it can't be determined (no context yet, or access denied) — the
    /// caller then falls back to the tick-freeze timeout. The process name is checked to guard against the OS
    /// reusing the pid for an unrelated process after the game closes.
    /// </summary>
    private static bool? IsGameProcessAlive(MumbleLinkSnapshot snapshot)
    {
        uint processId = snapshot.Context?.ProcessId ?? 0;
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            if (process.HasExited)
            {
                return false;
            }

            // GW2's executable is "Gw2-64.exe" → process name "Gw2-64"; reject a reused pid that isn't the game.
            return process.ProcessName.StartsWith("Gw2", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            // No process with that id is running.
            return false;
        }
        catch (InvalidOperationException)
        {
            // The process exited between lookup and inspection.
            return false;
        }
        catch (Exception)
        {
            // Couldn't determine (e.g. access denied) — let the caller fall back to the tick.
            return null;
        }
    }

    /// <summary>The account for a character (from the synced roster); falls back to the sole account if only one.</summary>
    private static async Task<string?> ResolveAccountIdAsync(Gw2GizmosDbContext db, string characterName, CancellationToken stoppingToken)
    {
        string? accountId = await db.Characters
            .Where(c => c.Name == characterName)
            .Select(c => c.AccountId)
            .FirstOrDefaultAsync(stoppingToken);
        if (!string.IsNullOrEmpty(accountId))
        {
            return accountId;
        }

        // The character isn't in the roster yet (Characters sync may be off). If exactly one account is synced,
        // it's unambiguous; otherwise we can't attribute the session.
        List<string> accountIds = await db.Accounts.Select(a => a.AccountId).Take(2).ToListAsync(stoppingToken);
        return accountIds.Count == 1 ? accountIds[0] : null;
    }

    /// <summary>
    /// Reconciles sessions left open by a previous run (a worker restart — common during development, or an app
    /// update). If the game is still running, the most recent open session is <b>resumed</b> as the current state
    /// so a continuous play session isn't fragmented (and its lagged loot still lands inside it). Any other open
    /// sessions (and the newest when the game isn't running) are closed at the last observed activity time — not at
    /// their start, which would zero the window — so their deltas stay meaningful.
    /// </summary>
    private async Task ResumeOrCloseOpenSessionsAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        List<GameSession> openSessions = await db.GameSessions.Where(s => s.EndedAtUtc == null).ToListAsync(stoppingToken);
        if (openSessions.Count == 0)
        {
            return;
        }

        // Newest = highest Id (avoid ordering by DateTimeOffset, which SQLite can't do server-side here).
        long newestId = openSessions.Max(s => s.Id);
        MumbleLinkSnapshot? snapshot = _reader.Read();
        bool gameRunning = snapshot is not null && IsGameProcessAlive(snapshot) == true;

        foreach (GameSession session in openSessions)
        {
            List<CharacterSegment> segments = (await db.CharacterSegments
                    .Where(s => s.GameSessionId == session.Id)
                    .ToListAsync(stoppingToken))
                .OrderBy(s => s.Sequence)
                .ToList();

            if (gameRunning && session.Id == newestId)
            {
                // Resume: adopt this session and its open segment as the live state; the tick loop continues it.
                CharacterSegment? openSegment = segments.LastOrDefault(s => s.EndedAtUtc is null);
                _gameSessionId = session.Id;
                _segmentId = openSegment?.Id;
                _activeCharacter = openSegment?.CharacterName;
                _nextSequence = segments.Count == 0 ? 0 : segments.Max(s => s.Sequence) + 1;
                _logger.LogInformation("Resumed open play session {Id} for {Account}.", session.Id, session.AccountId);
                continue;
            }

            // Close: end at the last activity we observed (best estimate of when the game actually stopped),
            // falling back to the latest segment boundary.
            DateTimeOffset endedAt = await LastActivityUtcAsync(db, session.AccountId, stoppingToken)
                ?? segments.Select(s => s.EndedAtUtc ?? s.StartedAtUtc).DefaultIfEmpty(session.StartedAtUtc).Max();
            foreach (CharacterSegment segment in segments.Where(s => s.EndedAtUtc is null))
            {
                segment.EndedAtUtc = endedAt;
            }

            session.EndedAtUtc = endedAt;
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    /// <summary>The most recent observation timestamp for the account, or null if there are none (max in memory —
    /// SQLite can't aggregate DateTimeOffset server-side here).</summary>
    private static async Task<DateTimeOffset?> LastActivityUtcAsync(Gw2GizmosDbContext db, string accountId, CancellationToken stoppingToken)
    {
        List<DateTimeOffset> itemTimes = await db.AccountItemObservations
            .Where(o => o.AccountId == accountId)
            .Select(o => o.ObservedAtUtc)
            .ToListAsync(stoppingToken);
        List<DateTimeOffset> walletTimes = await db.AccountWalletObservations
            .Where(o => o.AccountId == accountId)
            .Select(o => o.ObservedAtUtc)
            .ToListAsync(stoppingToken);

        IEnumerable<DateTimeOffset> all = itemTimes.Concat(walletTimes);
        return all.Any() ? all.Max() : null;
    }

    private void ResetState()
    {
        _gameSessionId = null;
        _segmentId = null;
        _activeCharacter = null;
        _nextSequence = 0;
    }
}
