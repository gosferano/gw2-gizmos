using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>An objective changing hands.</summary>
/// <param name="At">When it flipped (the objective's own flip time).</param>
/// <param name="From">Who held it before; null while neutral, or when it isn't known (a flip from before the page was
/// open).</param>
/// <param name="FromKnown">Whether <paramref name="From"/> is known.</param>
public sealed record WvwFlip(
    DateTimeOffset At,
    string ObjectiveId,
    string ObjectiveName,
    string ObjectiveType,
    int MapId,
    string MapName,
    WvwColor? From,
    bool FromKnown,
    WvwColor? To
);

/// <summary>
/// What's happening on the WvW maps, worked out from the matchups refreshed over time: each side's kills and deaths
/// per minute on each map, objectives flipped lately, objectives a team can take, and the flips seen.
/// </summary>
public sealed class WvwActivity
{
    /// <summary>The span kills and deaths per minute are measured over.</summary>
    public static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(5);

    /// <summary>A rate needs at least this much history to mean anything.</summary>
    public static readonly TimeSpan MinimumRateSpan = TimeSpan.FromMinutes(1);

    /// <summary>The span "flipped lately" counts over.</summary>
    public static readonly TimeSpan FlipWindow = TimeSpan.FromMinutes(10);

    /// <summary>How far back the feed starts when the page first sees a match.</summary>
    public static readonly TimeSpan FeedSeedWindow = TimeSpan.FromMinutes(30);

    private const int FeedLimit = 200;

    private readonly List<(DateTimeOffset At, WvwMatchup Matchup)> _samples = [];
    private readonly List<WvwFlip> _feed = [];
    private WvwMatchup? _last;

    /// <summary>The flips seen, newest first.</summary>
    public IReadOnlyList<WvwFlip> Feed => _feed;

    /// <summary>Takes in a refreshed matchup: keeps it for the rates and adds the flips since the last one.</summary>
    public void Add(WvwMatchup matchup, DateTimeOffset now)
    {
        if (_last is not null && _last.MatchId != matchup.MatchId)
        {
            // A new match (the weekly reset): nothing before it carries over.
            _samples.Clear();
            _feed.Clear();
            _last = null;
        }

        IEnumerable<WvwFlip> flips = _last is null ? Recent(matchup, now) : Changes(_last, matchup);
        _feed.InsertRange(0, flips.OrderByDescending(f => f.At));
        _feed.Sort((a, b) => b.At.CompareTo(a.At));
        if (_feed.Count > FeedLimit)
        {
            _feed.RemoveRange(FeedLimit, _feed.Count - FeedLimit);
        }

        _samples.Add((now, matchup));
        _samples.RemoveAll(s => now - s.At > RateWindow + MinimumRateSpan);
        _last = matchup;
    }

    /// <summary>
    /// A side's kills and deaths per minute on a map over the last few minutes; null until there's enough history.
    /// </summary>
    public (double Kills, double Deaths)? PerMinute(int mapId, WvwColor side, DateTimeOffset now)
    {
        if (_samples.Count < 2)
        {
            return null;
        }

        (DateTimeOffset at, WvwMatchup matchup) latest = _samples[^1];
        (DateTimeOffset at, WvwMatchup matchup) from = _samples.FirstOrDefault(s => latest.at - s.At <= RateWindow);
        TimeSpan span = latest.at - from.at;
        if (span < MinimumRateSpan)
        {
            return null;
        }

        WvwMatchMap? before = from.matchup.Maps.FirstOrDefault(m => m.MapId == mapId);
        WvwMatchMap? after = latest.matchup.Maps.FirstOrDefault(m => m.MapId == mapId);
        if (before is null || after is null)
        {
            return null;
        }

        double minutes = span.TotalMinutes;
        return (
            Math.Max(0, after.Kills[side] - before.Kills[side]) / minutes,
            Math.Max(0, after.Deaths[side] - before.Deaths[side]) / minutes
        );
    }

    /// <summary>How many of a map's objectives flipped within <see cref="FlipWindow"/>.</summary>
    public static int FlippedLately(WvwMatchup matchup, int mapId, DateTimeOffset now) =>
        matchup.Objectives.Count(o => o.MapId == mapId && now - o.LastFlipped <= FlipWindow);

    /// <summary>
    /// How many of a map's objectives <paramref name="team"/> can take now: held by another side or neutral, and not
    /// protected after a flip.
    /// </summary>
    public static int Capturable(WvwMatchup matchup, int mapId, WvwColor team, DateTimeOffset now) =>
        matchup.Objectives.Count(o => o.MapId == mapId && o.Owner != team && o.ProtectedFor(now) is null);

    /// <summary>The objectives whose flip time moved between two matchups, with who held them before.</summary>
    internal static IEnumerable<WvwFlip> Changes(WvwMatchup before, WvwMatchup after)
    {
        var known = before.Objectives.ToDictionary(o => o.Id);
        foreach (WvwObjectiveMarker objective in after.Objectives)
        {
            if (known.TryGetValue(objective.Id, out WvwObjectiveMarker? was) && objective.LastFlipped > was.LastFlipped)
            {
                yield return FlipOf(after, objective, was.Owner, fromKnown: true);
            }
        }
    }

    /// <summary>
    /// The flips within <see cref="FeedSeedWindow"/>, for a match first seen: who held them before isn't known.
    /// </summary>
    internal static IEnumerable<WvwFlip> Recent(WvwMatchup matchup, DateTimeOffset now) =>
        matchup
            .Objectives.Where(o => now - o.LastFlipped <= FeedSeedWindow)
            .Select(o => FlipOf(matchup, o, from: null, fromKnown: false));

    private static WvwFlip FlipOf(WvwMatchup matchup, WvwObjectiveMarker objective, WvwColor? from, bool fromKnown) =>
        new(
            objective.LastFlipped,
            objective.Id,
            objective.Name,
            objective.Type,
            objective.MapId,
            matchup.Maps.FirstOrDefault(m => m.MapId == objective.MapId)?.Name ?? "",
            from,
            fromKnown,
            objective.Owner
        );
}
