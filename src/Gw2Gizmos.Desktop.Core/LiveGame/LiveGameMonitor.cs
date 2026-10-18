using System;
using Gw2Gizmos.MumbleLink.Client;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>What the game is doing right now, as MumbleLink reports it.</summary>
/// <param name="IsRunning">The game is running: its MumbleLink tick is advancing.</param>
/// <param name="Character">The character in the world; null at character select or while loading.</param>
/// <param name="MapId">The map the character is on (0 when unknown).</param>
public sealed record LiveGameState(bool IsRunning, string? Character, int MapId)
{
    public static LiveGameState NotRunning { get; } = new(false, null, 0);

    /// <summary>
    /// Where the character is on Tyria's map, while in the world there; null at character select, while loading and in
    /// the Mists (WvW and PvP maps are on a continent of their own).
    /// </summary>
    public LivePosition? Position { get; init; }
}

/// <summary>A character's spot on the continent map.</summary>
/// <param name="X">In continent units, as the API's map positions.</param>
/// <param name="Y">In continent units, increasing southwards.</param>
/// <param name="Heading">The way the character faces, in radians clockwise from north.</param>
public sealed record LivePosition(double X, double Y, double Heading);

/// <summary>
/// Reads the game's MumbleLink directly (read-only) so the UI can show what's happening live — which character, on
/// which map — without waiting for the worker's session bookkeeping. Only a block whose tick is advancing counts as a
/// running game: a mapping left over from a game that has closed (or a bridge that was stopped) keeps its last data.
/// Not thread-safe: read from one thread (the UI's poll).
/// </summary>
public sealed class LiveGameMonitor : IDisposable
{
    /// <summary>How long the tick may stand still before the game is taken as gone (alt-tab keeps it advancing).</summary>
    private static readonly TimeSpan FreezeTimeout = TimeSpan.FromSeconds(30);

    private readonly IMumbleLinkReader? _reader;
    private uint? _lastTick;
    private DateTimeOffset _lastTickChange;
    private bool _tickAdvanced;

    /// <param name="reader">The MumbleLink source; null where there is none (the state is then always "not running").</param>
    public LiveGameMonitor(IMumbleLinkReader? reader)
    {
        _reader = reader;
    }

    public void Dispose() => _reader?.Dispose();

    public LiveGameState Read()
    {
        MumbleLinkSnapshot? snapshot = _reader?.Read();
        if (snapshot is null)
        {
            _lastTick = null;
            _tickAdvanced = false;
            return LiveGameState.NotRunning;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (snapshot.UiTick != _lastTick)
        {
            // The first reading only sets the baseline: a stale block hasn't proven it's live yet.
            _tickAdvanced = _lastTick is not null;
            _lastTick = snapshot.UiTick;
            _lastTickChange = now;
        }

        if (!_tickAdvanced || now - _lastTickChange > FreezeTimeout)
        {
            return LiveGameState.NotRunning;
        }

        string? character = string.IsNullOrEmpty(snapshot.Identity?.Name) ? null : snapshot.Identity!.Name;
        return new LiveGameState(true, character, (int)(snapshot.Context?.MapId ?? 0))
        {
            Position = character is null ? null : PositionOf(snapshot),
        };
    }

    private static LivePosition? PositionOf(MumbleLinkSnapshot snapshot)
    {
        if (snapshot.Context is not { } context || IsInTheMists(context.MapType))
        {
            return null;
        }

        if (context.PlayerX == 0 && context.PlayerY == 0)
        {
            return null;
        }

        // The game's world has Z pointing north, while continent Y points south.
        Vector3F front = snapshot.Avatar.Front;
        return new LivePosition(context.PlayerX, context.PlayerY, Math.Atan2(front.X, front.Z));
    }

    private static bool IsInTheMists(ContextMapType type) =>
        type.IsWorldVsWorld
        || type == ContextMapType.Pvp
        || type == ContextMapType.Gvg
        || type == ContextMapType.Tournament
        || type == ContextMapType.UserTournament
        || type == ContextMapType.ObsidianSanctum
        || type == ContextMapType.WvwLounge;
}
