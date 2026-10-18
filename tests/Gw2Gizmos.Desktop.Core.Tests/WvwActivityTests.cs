namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>What's happening on the WvW maps, from matchups refreshed over time.</summary>
public sealed class WvwActivityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 16, 0, 0, TimeSpan.Zero);

    private static WvwObjectiveMarker Objective(string id, WvwColor? owner, DateTimeOffset flipped, int mapId = 38) =>
        new(id, $"Objective {id}", "Tower", mapId, 0, 0, null, "[&x]", owner, owner?.ToString(), flipped, null, 0, 0);

    private static WvwMatchup Matchup(int redKills, params WvwObjectiveMarker[] objectives) =>
        new(
            "2-3",
            [
                new WvwTeamStanding(WvwColor.Green, 12010, "Green team", false, 0, 0, 0, 0, 0),
                new WvwTeamStanding(WvwColor.Red, 12003, "Red team", true, 0, 0, 0, 0, 0),
                new WvwTeamStanding(WvwColor.Blue, 12008, "Blue team", false, 0, 0, 0, 0, 0),
            ],
            1,
            Now,
            Now,
            [
                new WvwMatchMap(38, null, "Eternal Battlegrounds", 0, 0, 1, 1)
                {
                    Kills = new WvwSideValues(redKills, 0, 0),
                    Deaths = new WvwSideValues(0, redKills / 2, 0),
                },
            ],
            objectives
        );

    [Fact]
    public void Kills_and_deaths_per_minute_come_from_the_change_over_the_last_minutes()
    {
        var activity = new WvwActivity();
        activity.Add(Matchup(100), Now.AddMinutes(-4));
        activity.Add(Matchup(120), Now.AddMinutes(-2));
        activity.Add(Matchup(140), Now);

        (double kills, double deaths)? red = activity.PerMinute(38, WvwColor.Red, Now);
        (double kills, double deaths)? blue = activity.PerMinute(38, WvwColor.Blue, Now);

        Assert.Equal(10, red?.kills);
        Assert.Equal(5, blue?.deaths);
    }

    [Fact]
    public void There_is_no_rate_until_there_is_a_minute_of_history()
    {
        var activity = new WvwActivity();
        activity.Add(Matchup(100), Now.AddSeconds(-30));
        activity.Add(Matchup(110), Now);

        Assert.Null(activity.PerMinute(38, WvwColor.Red, Now));
    }

    [Fact]
    public void Capturable_counts_other_sides_and_neutral_objectives_not_protected_after_a_flip()
    {
        WvwMatchup matchup = Matchup(
            0,
            Objective("ours", WvwColor.Red, Now.AddHours(-1)),
            Objective("theirs", WvwColor.Blue, Now.AddHours(-1)),
            Objective("just-taken", WvwColor.Green, Now.AddMinutes(-2)),
            Objective("neutral", null, Now.AddHours(-1)),
            Objective("elsewhere", WvwColor.Blue, Now.AddHours(-1), mapId: 95)
        );

        Assert.Equal(2, WvwActivity.Capturable(matchup, 38, WvwColor.Red, Now));
        Assert.Equal(1, WvwActivity.FlippedLately(matchup, 38, Now));
    }

    [Fact]
    public void The_feed_starts_with_recent_flips_and_then_adds_each_flip_with_who_held_it_before()
    {
        var activity = new WvwActivity();
        activity.Add(
            Matchup(
                0,
                Objective("a", WvwColor.Blue, Now.AddMinutes(-20)),
                Objective("b", WvwColor.Red, Now.AddHours(-2))
            ),
            Now
        );
        activity.Add(
            Matchup(
                0,
                Objective("a", WvwColor.Blue, Now.AddMinutes(-20)),
                Objective("b", WvwColor.Green, Now.AddSeconds(10))
            ),
            Now.AddSeconds(15)
        );

        Assert.Equal(["b", "a"], activity.Feed.Select(f => f.ObjectiveId));
        WvwFlip taken = activity.Feed[0];
        Assert.Equal((WvwColor.Red, true, WvwColor.Green), (taken.From, taken.FromKnown, taken.To));
        Assert.False(activity.Feed[1].FromKnown);
    }
}
