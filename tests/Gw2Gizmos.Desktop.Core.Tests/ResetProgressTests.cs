using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The Daily and weekly page's reading of the synced progress: names and scheduled events for the API's ids, and
/// progress from before a reset counting as gone (the reset cleared it) until the next sync.
/// </summary>
public sealed class ResetProgressTests
{
    // Tuesday 20 October 2026, 15:00 UTC: the daily reset was at 00:00, the weekly on Monday 19th at 07:30.
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Today = Now.AddHours(-1);
    private static readonly DateTimeOffset Yesterday = Now.AddDays(-1);

    private readonly List<AccountResetActivity> _activities = [];
    private readonly List<AccountWizardsVaultObjective> _objectives = [];
    private readonly List<AccountWizardsVaultMeta> _metas = [];
    private readonly Dictionary<int, AccountAchievement> _progress = [];

    private ResetProgressData Data(Dictionary<int, string>? itemNames = null) =>
        new(_activities, _objectives, _metas, itemNames ?? new Dictionary<int, string>());

    private void Activity(string kind, string id, bool done, DateTimeOffset synced, string? wing = null,
        bool checkpoint = false) =>
        _activities.Add(new AccountResetActivity
        {
            Kind = kind, ActivityId = id, Done = done, SyncedAtUtc = synced, Group = wing, IsCheckpoint = checkpoint,
            Order = _activities.Count,
        });

    [Fact]
    public void Activities_take_their_names_and_events_from_the_kept_data()
    {
        Activity(ResetActivityKind.WorldBoss, "tequatl_the_sunless", true, Today);
        Activity(ResetActivityKind.WorldBoss, "a_new_boss", false, Today);
        Activity(ResetActivityKind.MapChest, "dragons_stand_heros_choice_chest", false, Today);
        Activity(ResetActivityKind.DailyCrafting, "lump_of_mithrilium", false, Today);

        ResetProgressData data = Data(new Dictionary<int, string> { [46742] = "Lump of Mithrillium" });
        IReadOnlyList<ResetActivity> bosses = ResetProgress.Activities(data, ResetActivityKind.WorldBoss, Now);
        ResetActivity chest = Assert.Single(ResetProgress.Activities(data, ResetActivityKind.MapChest, Now));
        ResetActivity craft = Assert.Single(ResetProgress.Activities(data, ResetActivityKind.DailyCrafting, Now));

        Assert.Equal(("Tequatl the Sunless", true), (bosses[0].Name, bosses[0].Done));
        Assert.Equal(["tequatl"], bosses[0].Events.Select(e => e.Id));
        Assert.Equal("A New Boss", bosses[1].Name); // not kept yet: named from its id
        Assert.Empty(bosses[1].Events);
        Assert.Equal("Dragon's Stand", chest.Name);
        Assert.Equal(["dragons-stand"], chest.Events.Select(e => e.Id));
        Assert.Equal(("Lump of Mithrillium", 46742), (craft.Name, craft.ItemId));
    }

    [Fact]
    public void An_activity_done_before_the_daily_reset_is_not_done_now()
    {
        Activity(ResetActivityKind.WorldBoss, "tequatl_the_sunless", true, Yesterday);

        Assert.False(Assert.Single(ResetProgress.Activities(Data(), ResetActivityKind.WorldBoss, Now)).Done);
    }

    [Fact]
    public void Raid_wings_count_their_bosses_cleared_since_the_weekly_reset()
    {
        DateTimeOffset beforeTheWeekly = new(2026, 10, 19, 7, 0, 0, TimeSpan.Zero);
        Activity(ResetActivityKind.Raid, "vale_guardian", true, Today, "spirit_vale");
        Activity(ResetActivityKind.Raid, "spirit_woods", true, Today, "spirit_vale", checkpoint: true);
        Activity(ResetActivityKind.Raid, "gorseval", false, Today, "spirit_vale");
        Activity(ResetActivityKind.Raid, "cairn", true, beforeTheWeekly, "bastion_of_the_penitent");

        IReadOnlyList<ResetActivityGroup> wings = ResetProgress.Raids(Data(), Now);

        Assert.Equal(
            [("Spirit Vale", 1, 2), ("Bastion of the Penitent", 0, 1)],
            wings.Select(w => (w.Name, w.Cleared, w.Activities.Count))
        );
    }

    [Fact]
    public void Dungeons_count_their_paths_completed_since_the_daily_reset()
    {
        Activity(ResetActivityKind.Dungeon, "se_story", true, Today, "sorrows_embrace");
        Activity(ResetActivityKind.Dungeon, "fergg", true, Yesterday, "sorrows_embrace");

        ResetActivityGroup dungeon = Assert.Single(ResetProgress.Dungeons(Data(), Now));

        Assert.Equal(("Sorrow's Embrace", 1), (dungeon.Name, dungeon.Cleared));
        Assert.Equal(["Story", "Fergg"], dungeon.Activities.Select(p => p.Name));
    }

    /// <summary>Synced rotating categories with their achievements (ids from 1), and the account's progress.</summary>
    private ResetProgressData Rotating(
        params (int CategoryId, string Name, (string Name, bool Weekly)[] Achievements)[] categories)
    {
        var achievements = new Dictionary<int, Achievement>();
        var stored = new List<AchievementCategory>();
        int id = 1;
        foreach ((int categoryId, string name, (string Name, bool Weekly)[] list) in categories)
        {
            var ids = new List<int>();
            foreach ((string achievementName, bool weekly) in list)
            {
                achievements[id] = new Achievement
                {
                    Id = id, Name = achievementName, Flags = weekly ? ["Weekly"] : ["Daily"],
                    Tiers = [new AchievementTier { Count = 3 }],
                };
                ids.Add(id++);
            }

            stored.Add(new AchievementCategory { Id = categoryId, Name = name, AchievementIds = ids.ToArray() });
        }

        return Data() with
        {
            RotatingCategories = stored,
            Achievements = achievements,
            AchievementProgress = _progress,
        };
    }

    [Fact]
    public void Rotating_achievements_show_this_resets_progress()
    {
        _progress[1] = new AccountAchievement { AchievementId = 1, Done = true, SyncedAtUtc = Today };
        _progress[2] = new AccountAchievement { AchievementId = 2, Current = 2, Max = 3, SyncedAtUtc = Today };
        _progress[3] = new AccountAchievement { AchievementId = 3, Done = true, SyncedAtUtc = Yesterday };
        _progress[4] = new AccountAchievement { AchievementId = 4, Done = true, SyncedAtUtc = Yesterday };

        ResetProgressData data = Rotating(
            (88, "Daily Fractals", [("Tier 4", false), ("Recommended", false), ("Tier 1", false)]),
            (464, "Weekly Rift Hunting", [("Rifts", true)]));
        IReadOnlyList<RotatingCategory> categories = ResetProgress.RotatingAchievements(data, null, Now);

        RotatingCategory fractals = categories[0];
        Assert.Equal(
            [("Tier 4", true, 0), ("Recommended", false, 2), ("Tier 1", false, 0)], // yesterday's progress is gone
            fractals.Achievements.Select(a => (a.Name, a.Done, a.Current))
        );
        Assert.True(categories[1].Achievements[0].Done); // weekly: done after Monday's reset, before today's
        Assert.Equal("Weekly Rift Hunting: Visions of Eternity", categories[1].Name); // told apart by expansion
    }

    [Fact]
    public void A_festivals_dailies_show_only_while_its_the_festival_chosen()
    {
        ResetProgressData data = Rotating(
            (88, "Daily Fractals", [("Tier 4", false)]),
            (79, "Halloween Daily", [("Shut and Locked", false)]));

        Assert.Equal(["Daily Fractals"], ResetProgress.RotatingAchievements(data, null, Now).Select(c => c.Name));
        Assert.Equal(
            ["Daily Fractals", "Halloween Daily"],
            ResetProgress.RotatingAchievements(data, Festival.Halloween, Now).Select(c => c.Name)
        );
        Assert.Equal(
            ["Daily Fractals"],
            ResetProgress.RotatingAchievements(data, Festival.DragonBash, Now).Select(c => c.Name)
        );
    }

    [Fact]
    public void A_wizards_vault_set_synced_before_its_reset_is_stale_and_gives_no_acclaim()
    {
        _metas.Add(new AccountWizardsVaultMeta
        {
            Period = WizardsVaultPeriod.Daily, ProgressComplete = 4, RewardAstral = 20, SyncedAtUtc = Yesterday,
        });
        _objectives.Add(new AccountWizardsVaultObjective
        {
            Period = WizardsVaultPeriod.Daily, Title = "Complete 3 Events", Acclaim = 10, ProgressComplete = 3,
            SyncedAtUtc = Yesterday,
        });

        WizardsVaultSet daily = ResetProgress.WizardsVault(Data(), WizardsVaultPeriod.Daily, Now)!;

        Assert.True(daily.IsStale);
        Assert.Equal(0, daily.AcclaimLeft);
    }

    [Fact]
    public void A_wizards_vault_set_counts_done_and_unclaimed_acclaim()
    {
        _metas.Add(new AccountWizardsVaultMeta
        {
            Period = WizardsVaultPeriod.Weekly, ProgressComplete = 6, RewardAstral = 50, SyncedAtUtc = Today,
        });
        _objectives.AddRange(
        [
            new() { Period = WizardsVaultPeriod.Weekly, ObjectiveId = 1, Title = "Claimed", Acclaim = 50,
                ProgressCurrent = 1, ProgressComplete = 1, Claimed = true, SyncedAtUtc = Today },
            new() { Period = WizardsVaultPeriod.Weekly, ObjectiveId = 2, Title = "Ready", Acclaim = 50,
                ProgressCurrent = 5, ProgressComplete = 5, SyncedAtUtc = Today },
            new() { Period = WizardsVaultPeriod.Weekly, ObjectiveId = 3, Title = "Started", Acclaim = 30,
                ProgressCurrent = 1, ProgressComplete = 4, SyncedAtUtc = Today },
        ]);

        WizardsVaultSet weekly = ResetProgress.WizardsVault(Data(), WizardsVaultPeriod.Weekly, Now)!;

        Assert.False(weekly.IsStale);
        Assert.Equal(2, weekly.Done); // claimed, and done but not yet claimed
        Assert.Equal(50 + 30 + 50, weekly.AcclaimLeft); // the unclaimed objectives and the meta reward
        Assert.True(weekly.Objectives[1].ReadyToClaim);
    }

    [Fact]
    public void The_special_objectives_last_past_a_reset()
    {
        _objectives.Add(new AccountWizardsVaultObjective
        {
            Period = WizardsVaultPeriod.Special, Title = "Log in", Acclaim = 5, ProgressComplete = 1,
            SyncedAtUtc = Yesterday,
        });

        WizardsVaultSet special = ResetProgress.WizardsVault(Data(), WizardsVaultPeriod.Special, Now)!;

        Assert.False(special.IsStale);
        Assert.Null(special.Meta);
    }

    [Fact]
    public void A_section_lists_whats_left_soonest_first_then_whats_done()
    {
        DateTimeOffset at = new(2026, 10, 20, 2, 20, 0, TimeSpan.Zero); // Claw of Jormag at 02:30, Shatterer at 04:00
        Activity(ResetActivityKind.WorldBoss, "tequatl_the_sunless", true, at);
        Activity(ResetActivityKind.WorldBoss, "the_shatterer", false, at);
        Activity(ResetActivityKind.WorldBoss, "claw_of_jormag", false, at);
        Activity(ResetActivityKind.WorldBoss, "a_new_boss", false, at);
        var section = new ResetActivitySection("World bosses", "today");

        section.Set(ResetProgress.Activities(Data(), ResetActivityKind.WorldBoss, at), at);

        Assert.Equal(
            ["Claw of Jormag", "The Shatterer", "A New Boss", "Tequatl the Sunless"],
            section.Rows.Select(r => r.Name)
        );
        Assert.Equal("1 of 4 today", section.Summary);
        Assert.Equal("in 10:00", section.Rows[0].Countdown);
    }

    [Fact]
    public void A_map_meta_not_on_a_timer_can_be_done_any_time_and_follows_the_scheduled_ones()
    {
        Activity(ResetActivityKind.MapChest, "gyala_delve_heros_choice_chest", false, Today);
        Activity(ResetActivityKind.MapChest, "dragons_stand_heros_choice_chest", false, Today);
        var section = new ResetActivitySection("Map chests", "today");

        section.Set(ResetProgress.Activities(Data(), ResetActivityKind.MapChest, Now), Now);

        Assert.Equal(["Dragon's Stand", "Gyala Delve"], section.Rows.Select(r => r.Name));
        Assert.Equal("any time", section.Rows[1].Countdown);
    }

    [Fact]
    public void The_convergences_shared_chest_follows_whichever_convergence_is_next()
    {
        DateTimeOffset at = new(2026, 10, 20, 0, 20, 0, TimeSpan.Zero); // Mount Balrior's 00:00 run has ended
        Activity(ResetActivityKind.MapChest, "convergence_heros_choice_chest", false, at);

        var row = new ResetActivityRow(
            Assert.Single(ResetProgress.Activities(Data(), ResetActivityKind.MapChest, at)), at);

        Assert.Equal(
            ("Convergence: Nexus of Eternity", "in 40:00", "[&BDYQAAA=]"), (row.Name, row.Countdown, row.ChatLink));
    }

    [Theory]
    [InlineData(3 * 24 + 10, 5, "3d 10h")]
    [InlineData(5, 42, "5h 42m")]
    [InlineData(0, 12, "12m")]
    public void A_time_left_reads_in_its_largest_units(int hours, int minutes, string text)
    {
        Assert.Equal(text, ResetProgressViewModel.Span(new TimeSpan(hours, minutes, 0)));
    }
}
