using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The Achievements page's list: each achievement's progress, tiers and points, which achievements are listed, the
/// scope tree, and the filters and sorts (closest to next tier finds quick wins across categories).
/// </summary>
public sealed class AchievementsCatalogTests
{
    private readonly Dictionary<int, Achievement> _achievements = new();
    private readonly Dictionary<int, AccountAchievement> _progress = new();
    private readonly Dictionary<int, AchievementCategory> _categories = new();
    private readonly List<AchievementGroup> _groups = new();

    private Achievement Add(int id, string name, params (int Count, int Points)[] tiers)
    {
        var achievement = new Achievement
        {
            Id = id,
            Name = name,
            Requirement = "Kill  centaurs.",
            Type = "Default",
            Tiers = tiers.Select(t => new AchievementTier { Count = t.Count, Points = t.Points }).ToList(),
        };
        _achievements[id] = achievement;
        return achievement;
    }

    private void Progress(int id, int current, bool done = false, int[]? bits = null, bool? unlocked = null) =>
        _progress[id] = new AccountAchievement
        {
            AchievementId = id, Current = current, Done = done, Bits = bits ?? [], Unlocked = unlocked,
        };

    private void Category(string groupName, int groupOrder, int id, string name, int order, params int[] achievements)
    {
        _categories[id] = new AchievementCategory
        {
            Id = id, Name = name, Order = order, AchievementIds = achievements,
        };
        AchievementGroup? group = _groups.FirstOrDefault(g => g.Name == groupName);
        if (group is null)
        {
            group = new AchievementGroup { Id = groupName, Name = groupName, Order = groupOrder };
            _groups.Add(group);
        }

        group.CategoryIds = [.. group.CategoryIds, id];
    }

    private AchievementsCatalog Build() =>
        AchievementsCatalog.Build(
            new AchievementsData(_groups, _categories, _achievements, _progress, new Dictionary<int, string>
            {
                [70502] = "Svanir Legguard Remnant",
                [70598] = "Mistward Legguards",
            })
        );

    private AchievementRow Row(int id) => Build().Rows.Single(row => row.Id == id);

    [Fact]
    public void A_row_shows_its_progress_toward_the_next_tier_and_the_points_earned_of_the_total()
    {
        Add(1, "Centaur Slayer", (10, 1), (100, 5), (500, 5), (1000, 5));
        Category("General", 0, 1, "Slayer", 10, 1);
        Progress(1, current: 120);

        AchievementRow row = Row(1);

        Assert.Equal(AchievementStatus.InProgress, row.Status);
        Assert.Equal((3, 500, 380), (row.NextTier, row.NextTierCount, row.ToNextTier));
        Assert.Equal((6, 16), (row.PointsEarned, row.PointsTotal));
        Assert.Equal("6 / 16 AP", row.PointsText);
        Assert.Equal($"{120:N0} / {500:N0} · tier 3 of 4", row.ProgressText);
        Assert.Equal($"Kill {1000:N0} centaurs.", row.Requirement);
        Assert.Equal([0.01, 0.1, 0.5], row.TierMarks);
        Assert.True(row.IsQuickWinCandidate);
    }

    [Fact]
    public void A_finished_achievement_has_every_point_and_objective()
    {
        Achievement collection = Add(2258, "Mistward Legguards", (1, 1), (2, 1));
        collection.Bits =
        [
            new AchievementBit { Type = "Item", Id = 70502 },
            new AchievementBit { Type = "Text", Text = "Defeat the Mark II golem." },
        ];
        collection.Rewards = [new AchievementReward { Type = "Item", Id = 70598, Count = 1 }];
        Category("Collections", 0, 1, "Legendary", 0, 2258);
        Progress(2258, current: 2, done: true);

        AchievementRow row = Row(2258);

        Assert.Equal(("Done", 2, 2), (row.ProgressText, row.PointsEarned, row.PointsTotal));
        Assert.Equal(
            [new AchievementObjective("Svanir Legguard Remnant", true, 70502), new("Defeat the Mark II golem.", true)],
            row.Objectives
        );
        Assert.Equal([new AchievementRewardLine("1× Mistward Legguards", 70598)], row.Rewards);
        Assert.False(row.IsQuickWinCandidate);
    }

    [Fact]
    public void Skin_and_mini_objectives_show_their_names_and_a_mini_its_item()
    {
        Achievement collection = Add(1, "Wardrobe", (2, 1));
        collection.Bits =
        [
            new AchievementBit { Type = "Skin", Id = 208 },
            new AchievementBit { Type = "Minipet", Id = 1 },
            new AchievementBit { Type = "Skin", Id = 999 },
        ];
        Category("Collections", 0, 1, "Misc", 0, 1);
        var data = new AchievementsData(_groups, _categories, _achievements, _progress, new Dictionary<int, string>())
        {
            SkinNames = new Dictionary<int, string> { [208] = "Scale Legs" },
            MiniNames = new Dictionary<int, string> { [1] = "Mini Rytlock" },
            MiniItemIds = new Dictionary<int, int> { [1] = 21047 },
        };

        AchievementRow row = Assert.Single(AchievementsCatalog.Build(data).Rows);

        Assert.Equal(["Scale Legs", "Mini Rytlock", "Skin 999"], row.Objectives.Select(o => o.Text));
        Assert.Equal([0, 21047, 0], row.Objectives.Select(o => o.ItemId));
    }

    [Fact]
    public void A_collection_marks_the_objectives_done_by_index()
    {
        Achievement collection = Add(2258, "Mistward Legguards", (1, 1), (3, 1));
        collection.Bits =
        [
            new AchievementBit { Type = "Text", Text = "A" },
            new AchievementBit { Type = "Text", Text = "B" },
            new AchievementBit { Type = "Text", Text = "C" },
        ];
        Category("Collections", 0, 1, "Legendary", 0, 2258);
        Progress(2258, current: 2, bits: [0, 2]);

        Assert.Equal([true, false, true], Row(2258).Objectives.Select(o => o.Done));
    }

    [Fact]
    public void An_achievement_needing_an_unlock_or_unfinished_prerequisites_is_locked()
    {
        Add(1, "Needs unlock", (1, 1)).Flags = ["RequiresUnlock"];
        Add(2, "Unlocked", (1, 1)).Flags = ["RequiresUnlock"];
        Add(3, "After 1", (1, 1)).Prerequisites = [1];
        Category("General", 0, 1, "Misc", 0, 1, 2, 3);
        Progress(2, current: 0, unlocked: true);

        AchievementsCatalog catalog = Build();

        Assert.Equal(
            [(1, true), (2, false), (3, true)],
            catalog.Rows.Select(row => (row.Id, row.IsLocked))
        );
        Assert.Equal("Locked", catalog.Rows[0].ProgressText);
        Assert.Equal(
            ["Not unlocked yet.", "", "Complete its prerequisites first."],
            catalog.Rows.Select(row => row.LockedText)
        );
    }

    [Fact]
    public void A_repeatable_counts_its_repeats_up_to_its_point_cap()
    {
        Achievement repeatable = Add(1, "Repeat", (10, 2), (20, 3));
        repeatable.Flags = ["Repeatable"];
        repeatable.PointCap = 12;
        Category("General", 0, 1, "Misc", 0, 1);
        _progress[1] = new AccountAchievement { AchievementId = 1, Current = 10, Repeated = 2, Bits = [] };

        AchievementRow row = Row(1);

        Assert.Equal((12, 12), (row.PointsEarned, row.PointsTotal)); // 2 + 2 × 5, capped at 12
        Assert.False(row.IsQuickWinCandidate);
    }

    [Fact]
    public void Hidden_unstarted_achievements_are_left_out_and_one_in_two_categories_is_listed_once()
    {
        Add(1, "Daily", (1, 1)).Flags = ["Daily"];
        Add(2, "Secret", (1, 1)).Flags = ["Hidden"];
        Add(3, "Found secret", (1, 1)).Flags = ["Hidden"];
        Add(4, "Shared", (1, 1));
        Add(5, "Plain", (1, 1));
        Category("Second", 1, 20, "Later", 0, 4);
        Category("First", 0, 10, "Earlier", 0, 1, 2, 3, 5, 4);
        Progress(3, current: 0);

        AchievementsCatalog catalog = Build();

        Assert.Equal([1, 3, 5, 4], catalog.Rows.Select(row => row.Id));
        Assert.Equal(["First", "Second"], catalog.Root.Children.Select(group => group.Name));
        Assert.Equal("Earlier", catalog.Rows.Single(row => row.Id == 4).CategoryName);
        Assert.Equal([4], catalog.Root.Children[1].AchievementIds);
    }

    [Fact]
    public void Hidden_unstarted_achievements_are_listed_when_asked_for()
    {
        Add(1, "Plain", (1, 1));
        Add(2, "Secret", (1, 1)).Flags = ["Hidden"];
        Category("General", 0, 1, "Misc", 0, 1, 2);

        AchievementsCatalog catalog = AchievementsCatalog.Build(
            new AchievementsData(_groups, _categories, _achievements, _progress, new Dictionary<int, string>()),
            includeHidden: true
        );

        Assert.Equal([(1, false), (2, true)], catalog.Rows.Select(row => (row.Id, row.IsHidden)));
        Assert.Equal("0 / 2", catalog.Root.CountText);
    }

    [Fact]
    public void The_scope_tree_counts_done_achievements_in_each_group_and_category()
    {
        Add(1, "A", (1, 1));
        Add(2, "B", (1, 1));
        Add(3, "C", (1, 1));
        Category("General", 0, 1, "Slayer", 1, 1, 2);
        Category("General", 0, 2, "Explorer", 0, 3);
        Progress(1, current: 1, done: true);

        AchievementScope root = Build().Root;

        Assert.Equal("1 / 3", root.CountText);
        AchievementScope general = Assert.Single(root.Children);
        Assert.Equal(["Explorer", "Slayer"], general.Children.Select(c => c.Name));
        Assert.Equal(["0 / 1", "1 / 2"], general.Children.Select(c => c.CountText));
    }

    [Fact]
    public void Merging_a_rebuilt_tree_keeps_its_nodes_and_what_is_open()
    {
        Add(1, "Plain", (1, 1));
        Add(2, "Secret", (1, 1)).Flags = ["Hidden"];
        Category("General", 0, 1, "Slayer", 0, 1);
        Category("General", 0, 2, "Secrets", 1, 2);
        var data = new AchievementsData(_groups, _categories, _achievements, _progress, new Dictionary<int, string>());

        AchievementScope shown = AchievementsCatalog.Build(data).Root;
        AchievementScope general = shown.Children[0];
        AchievementScope slayer = general.Children[0];
        general.IsExpanded = true;

        // Hidden ones shown: the Secrets category appears; Slayer gets its achievement done.
        _progress[1] = new AccountAchievement { AchievementId = 1, Current = 1, Done = true, Bits = [] };
        shown.MergeFrom(AchievementsCatalog.Build(data with { Progress = _progress }, includeHidden: true).Root);

        Assert.Same(general, shown.Children[0]);
        Assert.Same(slayer, general.Children[0]);
        Assert.True(general.IsExpanded);
        Assert.Equal(["Slayer", "Secrets"], general.Children.Select(c => c.Name));
        Assert.Equal("1 / 1", slayer.CountText);
        Assert.Equal("1 / 2", shown.CountText);

        // Hidden ones hidden again: Secrets goes, the rest stays.
        shown.MergeFrom(AchievementsCatalog.Build(data).Root);

        Assert.Same(slayer, Assert.Single(general.Children));
        Assert.True(general.IsExpanded);
    }

    [Fact]
    public void Closest_to_next_tier_puts_quick_wins_first_across_categories()
    {
        Add(1, "Far", (100, 1));
        Add(2, "Nearly", (20, 1));
        Add(3, "Done", (5, 1));
        Add(4, "Locked", (10, 1)).Flags = ["RequiresUnlock"];
        Add(5, "Halfway", (10, 1));
        Category("General", 0, 1, "Slayer", 0, 1, 3);
        Category("Fractals", 1, 2, "Fractals", 0, 2, 4, 5);
        Progress(1, current: 10);
        Progress(2, current: 18);
        Progress(3, current: 5, done: true);
        Progress(4, current: 9);
        Progress(5, current: 5);

        AchievementsCatalog catalog = Build();
        IReadOnlyList<AchievementRow> rows =
            catalog.Select(catalog.Root, AchievementFilter.All, AchievementSort.ClosestToNextTier, "");

        Assert.Equal([2, 5, 1, 3, 4], rows.Select(row => row.Id));
    }

    [Fact]
    public void Filters_and_search_narrow_the_list()
    {
        Add(1, "Centaur Slayer", (10, 1));
        Add(2, "Grawl Slayer", (10, 1));
        Add(3, "Explorer", (10, 1));
        Category("General", 0, 1, "Misc", 0, 1, 2, 3);
        Progress(1, current: 10, done: true);
        Progress(2, current: 3);

        AchievementsCatalog catalog = Build();
        List<int> Ids(AchievementFilter filter, string search = "") =>
            catalog.Select(catalog.Root, filter, AchievementSort.GameOrder, search).Select(row => row.Id).ToList();

        Assert.Equal([2, 3], Ids(AchievementFilter.Unfinished));
        Assert.Equal([2], Ids(AchievementFilter.InProgress));
        Assert.Equal([3], Ids(AchievementFilter.NotStarted));
        Assert.Equal([1], Ids(AchievementFilter.Finished));
        Assert.Equal([1, 2], Ids(AchievementFilter.All, " slayer "));
    }
}
