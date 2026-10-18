using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A side of a WvW match.</summary>
public enum WvwColor
{
    Red,
    Blue,
    Green,
}

/// <summary>Where one team stands in the match.</summary>
/// <param name="SkirmishScore">Its war score in the skirmish under way; 0 until the API lists that skirmish.</param>
/// <param name="PointsPerTick">
/// The war score its objectives earn every tick (five minutes), across all four maps.
/// </param>
public sealed record WvwTeamStanding(
    WvwColor Color,
    int TeamId,
    string Name,
    bool IsYours,
    int VictoryPoints,
    int SkirmishScore,
    int PointsPerTick,
    int Kills,
    int Deaths
)
{
    public bool IsRed => Color == WvwColor.Red;
    public bool IsBlue => Color == WvwColor.Blue;
    public bool IsGreen => Color == WvwColor.Green;
}

/// <summary>One of the match's maps, where it lies on the Mists.</summary>
/// <param name="Home">Whose borderland it is; null for Eternal Battlegrounds.</param>
public sealed record WvwMatchMap(
    int MapId,
    WvwColor? Home,
    string Name,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY
);

/// <summary>A WvW match as the WvW page shows it.</summary>
/// <param name="Skirmish">The skirmish under way, from 1.</param>
/// <param name="SkirmishEndsAt">When the skirmish under way ends (skirmishes last two hours).</param>
public sealed record WvwMatchup(
    string MatchId,
    IReadOnlyList<WvwTeamStanding> Teams,
    int Skirmish,
    DateTimeOffset SkirmishEndsAt,
    DateTimeOffset MatchEndsAt,
    IReadOnlyList<WvwMatchMap> Maps
)
{
    private static readonly TimeSpan SkirmishLength = TimeSpan.FromHours(2);

    /// <summary>
    /// Builds the matchup from the API's match, for the team <paramref name="yourTeam"/>, as it stands at
    /// <paramref name="now"/>. A team is found by its team id among each side's worlds; <paramref name="mapRects"/>
    /// gives the maps' places on the Mists, by map id. The skirmish under way is worked out from the clock: the API
    /// lists a skirmish late, so its last one may already be over.
    /// </summary>
    public static WvwMatchup From(
        WvwMatch match,
        int yourTeam,
        IReadOnlyDictionary<int, (double MinX, double MinY, double MaxX, double MaxY)> mapRects,
        DateTimeOffset now
    )
    {
        int skirmishCount = (int)Math.Ceiling((match.EndTime - match.StartTime) / SkirmishLength);
        int skirmishNumber = Math.Clamp(
            (int)Math.Floor((now - match.StartTime) / SkirmishLength) + 1,
            1,
            skirmishCount
        );
        WvwMatchTeamValues<int>? skirmish = match.Skirmishes.FirstOrDefault(s => s.Id == skirmishNumber)?.Scores;

        WvwTeamStanding Standing(WvwColor color)
        {
            int teamId = Pick(match.AllWorlds, color).FirstOrDefault(WvwTeams.IsTeam);
            WvwOwner owner = OwnerOf(color);
            int pointsPerTick = match
                .Maps.SelectMany(m => m.Objectives)
                .Where(o => o.Owner == owner)
                .Sum(o => o.PointsTick);
            return new WvwTeamStanding(
                color,
                teamId,
                WvwTeams.NameOf(teamId),
                teamId == yourTeam,
                Pick(match.VictoryPoints, color),
                skirmish is null ? 0 : Pick(skirmish, color),
                pointsPerTick,
                Pick(match.Kills, color),
                Pick(match.Deaths, color)
            );
        }

        var maps = new List<WvwMatchMap>();
        foreach (Gw2Gizmos.Gw2Api.Contract.V2.Wvw.WvwMatchMap map in match.Maps)
        {
            if (!mapRects.TryGetValue(map.Id, out var rect))
            {
                continue;
            }

            WvwColor? home =
                map.Type == WvwMapType.RedHome ? WvwColor.Red
                : map.Type == WvwMapType.BlueHome ? WvwColor.Blue
                : map.Type == WvwMapType.GreenHome ? WvwColor.Green
                : null;
            string name = home is { } color ? $"{color} Borderlands" : "Eternal Battlegrounds";
            maps.Add(new WvwMatchMap(map.Id, home, name, rect.MinX, rect.MinY, rect.MaxX, rect.MaxY));
        }

        // Eternal Battlegrounds first, then the borderlands in red, blue, green order.
        maps.Sort((a, b) => (a.Home is null ? -1 : (int)a.Home).CompareTo(b.Home is null ? -1 : (int)b.Home));

        DateTimeOffset skirmishEnd = match.StartTime + SkirmishLength * skirmishNumber;
        return new WvwMatchup(
            match.Id,
            [Standing(WvwColor.Red), Standing(WvwColor.Blue), Standing(WvwColor.Green)],
            skirmishNumber,
            skirmishEnd < match.EndTime ? skirmishEnd : match.EndTime,
            match.EndTime,
            maps
        );
    }

    private static T Pick<T>(WvwMatchTeamValues<T> values, WvwColor color)
        where T : notnull =>
        color switch
        {
            WvwColor.Red => values.Red,
            WvwColor.Blue => values.Blue,
            _ => values.Green,
        };

    private static WvwOwner OwnerOf(WvwColor color) =>
        color switch
        {
            WvwColor.Red => WvwOwner.Red,
            WvwColor.Blue => WvwOwner.Blue,
            _ => WvwOwner.Green,
        };
}
