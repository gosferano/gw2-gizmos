using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Turning the API's WvW match into the WvW page's matchup.</summary>
public sealed class WvwMatchupTests
{
    private static WvwMatchTeamValues<T> Values<T>(T red, T blue, T green)
        where T : notnull =>
        new()
        {
            Red = red,
            Blue = blue,
            Green = green,
        };

    private static WvwMatchMapObjective Objective(WvwOwner owner, int pointsTick) =>
        new()
        {
            Id = "1-1",
            Owner = owner,
            PointsTick = pointsTick,
        };

    private static Gw2Gizmos.Gw2Api.Contract.V2.Wvw.WvwMatchMap Map(
        int id,
        WvwMapType type,
        params WvwMatchMapObjective[] objectives
    ) =>
        new()
        {
            Id = id,
            Type = type,
            Scores = Values(0, 0, 0),
            Objectives = objectives,
        };

    private static readonly WvwMatch Match = new()
    {
        Id = "2-3",
        StartTime = new DateTimeOffset(2026, 10, 16, 18, 0, 0, TimeSpan.Zero),
        EndTime = new DateTimeOffset(2026, 10, 23, 18, 0, 0, TimeSpan.Zero),
        Scores = Values(0, 0, 0),
        Worlds = Values(2003, 2008, 2010),
        AllWorlds = Values(new[] { 12003, 2104, 2003 }, new[] { 2204, 12008, 2008 }, new[] { 12010, 2206, 2010 }),
        Kills = Values(10, 20, 30),
        Deaths = Values(1, 2, 3),
        VictoryPoints = Values(100, 90, 80),
        Maps =
        [
            Map(96, WvwMapType.BlueHome, Objective(WvwOwner.Blue, 5)),
            Map(
                38,
                WvwMapType.Center,
                Objective(WvwOwner.Red, 2),
                Objective(WvwOwner.Red, 10),
                Objective(WvwOwner.Green, 2)
            ),
            Map(1099, WvwMapType.RedHome, Objective(WvwOwner.Red, 5)),
            Map(95, WvwMapType.GreenHome),
        ],
        Skirmishes = [new() { Id = 1, Scores = Values(1, 1, 1) }, new() { Id = 2, Scores = Values(300, 200, 100) }],
    };

    private static readonly Dictionary<int, (double, double, double, double)> Rects = new()
    {
        [38] = (0, 0, 10, 10),
        [95] = (0, 0, 10, 10),
        [96] = (0, 0, 10, 10),
        [1099] = (0, 0, 10, 10),
    };

    [Fact]
    public void Teams_are_found_among_each_sides_worlds_with_their_standing()
    {
        WvwMatchup matchup = WvwMatchup.From(Match, yourTeam: 12008, Rects, DuringSkirmish2);

        Assert.Equal([12003, 12008, 12010], matchup.Teams.Select(t => t.TeamId));
        WvwTeamStanding red = matchup.Teams[0];
        Assert.Equal(
            ("Silent Woods", 100, 300, 17, 10, 1),
            (red.Name, red.VictoryPoints, red.SkirmishScore, red.PointsPerTick, red.Kills, red.Deaths)
        );
        Assert.True(matchup.Teams[1].IsYours);
        Assert.False(red.IsYours);
    }

    /// <summary>Half an hour into the second skirmish, which the match lists.</summary>
    private static readonly DateTimeOffset DuringSkirmish2 = Match.StartTime.AddHours(2.5);

    [Fact]
    public void The_skirmish_under_way_is_worked_out_from_the_clock()
    {
        WvwMatchup matchup = WvwMatchup.From(Match, yourTeam: 12008, Rects, DuringSkirmish2);

        Assert.Equal(2, matchup.Skirmish);
        Assert.Equal(Match.StartTime.AddHours(4), matchup.SkirmishEndsAt);
    }

    [Fact]
    public void A_skirmish_the_api_has_not_listed_yet_is_under_way_with_no_score()
    {
        // The match lists two skirmishes, but the third began at hour four.
        WvwMatchup matchup = WvwMatchup.From(Match, yourTeam: 12008, Rects, Match.StartTime.AddHours(4.5));

        Assert.Equal(3, matchup.Skirmish);
        Assert.Equal(Match.StartTime.AddHours(6), matchup.SkirmishEndsAt);
        Assert.All(matchup.Teams, team => Assert.Equal(0, team.SkirmishScore));
    }

    [Fact]
    public void Maps_come_eternal_battlegrounds_first_then_red_blue_green_borderlands()
    {
        WvwMatchup matchup = WvwMatchup.From(Match, yourTeam: 12008, Rects, DuringSkirmish2);

        Assert.Equal(
            ["Eternal Battlegrounds", "Red Borderlands", "Blue Borderlands", "Green Borderlands"],
            matchup.Maps.Select(m => m.Name)
        );
    }

    [Fact]
    public void Scored_objectives_get_their_place_holder_and_claiming_guild_but_ruins_are_left_out()
    {
        WvwMatchMapObjective castle = new()
        {
            Id = "38-9",
            Owner = WvwOwner.Green,
            LastFlipped = DuringSkirmish2.AddMinutes(-2),
            ClaimedBy = "guild-1",
            PointsTick = 12,
        };
        WvwMatchMapObjective ruin = new() { Id = "38-62", Owner = WvwOwner.Red };
        WvwMatch match = new()
        {
            Id = Match.Id,
            StartTime = Match.StartTime,
            EndTime = Match.EndTime,
            Scores = Match.Scores,
            Worlds = Match.Worlds,
            AllWorlds = Match.AllWorlds,
            Kills = Match.Kills,
            Deaths = Match.Deaths,
            VictoryPoints = Match.VictoryPoints,
            Skirmishes = Match.Skirmishes,
            Maps = [Map(38, WvwMapType.Center, castle, ruin)],
        };
        Dictionary<string, WvwObjective> objectives = new()
        {
            ["38-9"] = new()
            {
                Id = "38-9",
                Name = "Stonemist Castle",
                Type = WvwObjectiveType.Castle,
                MapId = 38,
                Coord = [10606.3m, 14580.3m, -1536.9m],
                ChatLink = "[&DAkAAAAmAAAA]",
            },
            ["38-62"] = new()
            {
                Id = "38-62",
                Name = "Temple of Lost Prayers",
                Type = "Ruins",
                MapId = 38,
                Coord = [1m, 2m],
                ChatLink = "[&x]",
            },
        };
        Dictionary<string, WvwGuild> guilds = new() { ["guild-1"] = new WvwGuild("Prôgrëss", "poG") };

        WvwMatchup matchup = WvwMatchup.From(match, 12008, Rects, DuringSkirmish2, objectives, guilds);

        WvwObjectiveMarker marker = Assert.Single(matchup.Objectives);
        Assert.Equal(
            ("Stonemist Castle", "Castle", WvwColor.Green, "Grenth's Door", 12),
            (marker.Name, marker.Type, marker.Owner, marker.OwnerName, marker.PointsTick)
        );
        Assert.Equal((10606.3, 14580.3), (marker.X, marker.Y));
        Assert.Equal("poG", marker.Guild?.Tag);
        Assert.Equal(TimeSpan.FromMinutes(3), marker.ProtectedFor(DuringSkirmish2));
    }

    [Theory]
    [InlineData(0, 0, null, "Secured", 15)]
    [InlineData(14, 0, null, "Secured", 1)]
    [InlineData(15, 1, "Secured", "Reinforced", 20)]
    [InlineData(34, 1, "Secured", "Reinforced", 1)]
    [InlineData(35, 2, "Reinforced", "Fortified", 35)]
    [InlineData(70, 3, "Fortified", null, null)]
    [InlineData(90, 3, "Fortified", null, null)]
    public void An_objectives_tier_counts_each_tiers_yaks_on_top_of_the_ones_before(
        int yaks,
        int tier,
        string? name,
        string? next,
        int? yaksToNext
    )
    {
        // A tower's upgrade set: 15, 20 and 35 yaks, so 70 in all for the top tier.
        WvwUpgrade tower = new()
        {
            Id = 35,
            Tiers =
            [
                new() { Name = "Secured", YaksRequired = 15 },
                new() { Name = "Reinforced", YaksRequired = 20 },
                new() { Name = "Fortified", YaksRequired = 35 },
            ],
        };

        Assert.Equal((tier, name, next, yaksToNext), WvwMatchup.TierOf(tower, yaks));
    }
}
