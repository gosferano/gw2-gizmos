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

/// <summary>A number for each side.</summary>
public sealed record WvwSideValues(int Red, int Blue, int Green)
{
    public static WvwSideValues Zero { get; } = new(0, 0, 0);

    public int this[WvwColor side] =>
        side switch
        {
            WvwColor.Red => Red,
            WvwColor.Blue => Blue,
            _ => Green,
        };
}

/// <summary>One of the match's maps, where it lies on the Mists, and each side's kills and deaths on it.</summary>
/// <param name="Home">Whose borderland it is; null for Eternal Battlegrounds.</param>
public sealed record WvwMatchMap(
    int MapId,
    WvwColor? Home,
    string Name,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY
)
{
    public WvwSideValues Kills { get; init; } = WvwSideValues.Zero;
    public WvwSideValues Deaths { get; init; } = WvwSideValues.Zero;
}

/// <summary>A WvW match as the WvW page shows it.</summary>
/// <param name="Teams">The three teams, in the order their borderlands lie on the map from left to right.</param>
/// <param name="Skirmish">The skirmish under way, from 1.</param>
/// <param name="SkirmishEndsAt">When the skirmish under way ends (skirmishes last two hours).</param>
public sealed record WvwMatchup(
    string MatchId,
    IReadOnlyList<WvwTeamStanding> Teams,
    int Skirmish,
    DateTimeOffset SkirmishEndsAt,
    DateTimeOffset MatchEndsAt,
    IReadOnlyList<WvwMatchMap> Maps,
    IReadOnlyList<WvwObjectiveMarker> Objectives
)
{
    /// <summary>The objectives drawn on the map: the ones the match scores.</summary>
    private static readonly HashSet<string> ShownObjectiveTypes = ["Camp", "Tower", "Keep", "Castle"];

    private static readonly TimeSpan SkirmishLength = TimeSpan.FromHours(2);

    /// <summary>
    /// Builds the matchup from the API's match, for the team <paramref name="yourTeam"/>, as it stands at
    /// <paramref name="now"/>. A team is found by its team id among each side's worlds; <paramref name="mapRects"/>
    /// gives the maps' places on the Mists, by map id. The skirmish under way is worked out from the clock: the API
    /// lists a skirmish late, so its last one may already be over. <paramref name="objectives"/> are the objectives'
    /// names and places by id (<c>/v2/wvw/objectives</c>), <paramref name="guilds"/> the claiming guilds by id,
    /// <paramref name="upgrades"/> the yak upgrade tiers by upgrade id (<c>/v2/wvw/upgrades</c>), and
    /// <paramref name="guildUpgrades"/> the guild upgrades' names by id.
    /// </summary>
    public static WvwMatchup From(
        WvwMatch match,
        int yourTeam,
        IReadOnlyDictionary<int, (double MinX, double MinY, double MaxX, double MaxY)> mapRects,
        DateTimeOffset now,
        IReadOnlyDictionary<string, WvwObjective>? objectives = null,
        IReadOnlyDictionary<string, WvwGuild>? guilds = null,
        IReadOnlyDictionary<int, WvwUpgrade>? upgrades = null,
        IReadOnlyDictionary<int, string>? guildUpgrades = null
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
            maps.Add(
                new WvwMatchMap(map.Id, home, name, rect.MinX, rect.MinY, rect.MaxX, rect.MaxY)
                {
                    Kills = map.Kills is { } kills
                        ? new WvwSideValues(kills.Red, kills.Blue, kills.Green)
                        : WvwSideValues.Zero,
                    Deaths = map.Deaths is { } deaths
                        ? new WvwSideValues(deaths.Red, deaths.Blue, deaths.Green)
                        : WvwSideValues.Zero,
                }
            );
        }

        // Eternal Battlegrounds first, then the borderlands in red, blue, green order.
        maps.Sort((a, b) => (a.Home is null ? -1 : (int)a.Home).CompareTo(b.Home is null ? -1 : (int)b.Home));

        DateTimeOffset skirmishEnd = match.StartTime + SkirmishLength * skirmishNumber;
        // The teams in the order their borderlands lie on the map, left to right: green, red, blue as the Mists are
        // laid out, kept as the order when a borderland's place isn't known.
        WvwColor[] order = [WvwColor.Green, WvwColor.Red, WvwColor.Blue];
        Dictionary<WvwColor, WvwMatchMap?> homes = order.ToDictionary(
            color => color,
            color => maps.FirstOrDefault(m => m.Home == color)
        );
        if (homes.Values.All(home => home is not null))
        {
            order = order.OrderBy(color => homes[color]!.MinX + homes[color]!.MaxX).ToArray();
        }

        WvwTeamStanding[] teams = order.Select(Standing).ToArray();
        return new WvwMatchup(
            match.Id,
            teams,
            skirmishNumber,
            skirmishEnd < match.EndTime ? skirmishEnd : match.EndTime,
            match.EndTime,
            maps,
            ObjectivesOf(match, objectives, guilds, upgrades, guildUpgrades, teams)
        );
    }

    private static List<WvwObjectiveMarker> ObjectivesOf(
        WvwMatch match,
        IReadOnlyDictionary<string, WvwObjective>? objectives,
        IReadOnlyDictionary<string, WvwGuild>? guilds,
        IReadOnlyDictionary<int, WvwUpgrade>? upgrades,
        IReadOnlyDictionary<int, string>? guildUpgrades,
        IReadOnlyList<WvwTeamStanding> teams
    )
    {
        var markers = new List<WvwObjectiveMarker>();
        if (objectives is null)
        {
            return markers;
        }

        foreach (WvwMatchMapObjective state in match.Maps.SelectMany(m => m.Objectives))
        {
            if (
                !objectives.TryGetValue(state.Id, out WvwObjective? objective)
                || !ShownObjectiveTypes.Contains(objective.Type)
                || objective.Coord.Length < 2
            )
            {
                continue;
            }

            WvwColor? owner =
                state.Owner == WvwOwner.Red ? WvwColor.Red
                : state.Owner == WvwOwner.Blue ? WvwColor.Blue
                : state.Owner == WvwOwner.Green ? WvwColor.Green
                : null;
            WvwGuild? guild =
                state.ClaimedBy is { } guildId && guilds is not null && guilds.TryGetValue(guildId, out WvwGuild? found)
                    ? found
                    : null;
            int yaks = state.YaksDelivered ?? 0;
            (int tier, string? tierName, string? nextTierName, int? yaksToNext) =
                objective.UpgradeId is { } upgradeId
                && upgrades is not null
                && upgrades.TryGetValue(upgradeId, out WvwUpgrade? upgrade)
                    ? TierOf(upgrade, yaks)
                    : (0, null, null, null);
            List<string> slotted = guildUpgrades is null
                ? []
                : state
                    .GuildUpgrades.Select(id => guildUpgrades.TryGetValue(id, out string? name) ? name : null)
                    .OfType<string>()
                    .ToList();
            markers.Add(
                new WvwObjectiveMarker(
                    objective.Id,
                    objective.Name,
                    objective.Type.ToString(),
                    objective.MapId,
                    (double)objective.Coord[0],
                    (double)objective.Coord[1],
                    string.IsNullOrEmpty(objective.Marker) ? null : objective.Marker,
                    objective.ChatLink,
                    owner,
                    owner is { } color ? teams.First(t => t.Color == color).Name : null,
                    state.LastFlipped,
                    guild,
                    state.PointsTick,
                    yaks,
                    tier,
                    tierName,
                    nextTierName,
                    yaksToNext,
                    slotted
                )
            );
        }

        return markers;
    }

    /// <summary>
    /// The tier an objective has reached with <paramref name="yaks"/> delivered. Each tier's yaks count on top of the
    /// tiers before it (a tower's 15, 20 and 35 make 70 for the top), and the tiers come in order.
    /// </summary>
    internal static (int Tier, string? Name, string? NextName, int? YaksToNext) TierOf(WvwUpgrade upgrade, int yaks)
    {
        int needed = 0;
        for (int i = 0; i < upgrade.Tiers.Length; i++)
        {
            needed += upgrade.Tiers[i].YaksRequired;
            if (yaks < needed)
            {
                return (i, i > 0 ? upgrade.Tiers[i - 1].Name : null, upgrade.Tiers[i].Name, needed - yaks);
            }
        }

        return (upgrade.Tiers.Length, upgrade.Tiers.LastOrDefault()?.Name, null, null);
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
