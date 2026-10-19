using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// The achievements sync stores the groups, categories and achievements of /v2/achievements, updating rows it has seen
/// before.
/// </summary>
public sealed class AchievementsUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();

    public void Dispose() => _fixture.Dispose();

    // As the API returns them: Centaur Slayer (tiers only) and a collection with bits, flags and an item reward.
    private const string CentaurSlayer = """
        {
          "id": 1, "name": "Centaur Slayer", "description": "A few more centaur herds are thinned out.",
          "requirement": "Kill  centaurs.", "locked_text": "", "type": "Default", "flags": ["Permanent"],
          "tiers": [{"count": 10, "points": 1}, {"count": 100, "points": 5}]
        }
        """;

    private const string Legguards = """
        {
          "id": 2258, "name": "Mistward Legguards", "description": "", "type": "ItemSet",
          "requirement": "Collect all  items and skins to forge your Mistward Legguards.",
          "locked_text": "Complete the Invocation revenant specialization to unlock the collection.",
          "flags": ["IgnoreNearlyComplete", "RequiresUnlock"], "prerequisites": [1],
          "bits": [
            {"type": "Skin", "id": 208, "text": ""}, {"type": "Item", "id": 70502, "text": "Dropped by Svanir."}
          ],
          "tiers": [{"count": 1, "points": 1}, {"count": 2, "points": 1}],
          "rewards": [{"type": "Item", "id": 70598, "count": 1}, {"type": "Mastery", "id": 3, "region": "Tyria"}]
        }
        """;

    private void StubApi(string centaurName = "Centaur Slayer")
    {
        _stub.SetJson(
            "/v2/achievements/groups",
            """
            [{"id": "56A82BB9-6B07-4AB0-89EE-E4A6D68F5C47", "name": "General", "description": "", "order": 0,
              "categories": [1]}]
            """
        );
        _stub.SetJson(
            "/v2/achievements/categories",
            """
            [{"id": 1, "name": "Slayer", "description": "", "order": 10, "icon": "https://x/slayer.png",
              "achievements": [{"id": 2258}, {"id": 1}]}]
            """
        );
        _stub.SetJson("/v2/achievements", "[1, 2258]");
        _stub.SetJson(
            "/v2/achievements?ids=1,2258",
            $"[{CentaurSlayer.Replace("Centaur Slayer", centaurName)}, {Legguards}]"
        );
    }

    private async Task SyncAsync()
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        var updater = new AchievementsUpdater(db, _stub.BuildClientFactory(), NullLogger<AchievementsUpdater>.Instance);
        await updater.UpdateAchievements(CancellationToken.None);
    }

    [Fact]
    public async Task Stores_groups_categories_and_achievements_with_their_tiers_bits_and_rewards()
    {
        StubApi();

        await SyncAsync();

        using Gw2GizmosDbContext db = _fixture.NewContext();
        AchievementGroup group = await db.AchievementGroups.SingleAsync();
        Assert.Equal("General", group.Name);
        Assert.Equal([1], group.CategoryIds);
        AchievementCategory category = await db.AchievementCategories.SingleAsync();
        Assert.Equal(("Slayer", 10), (category.Name, category.Order));
        Assert.Equal([2258, 1], category.AchievementIds);

        Achievement collection = await db.Achievements.SingleAsync(a => a.Id == 2258);
        Assert.Equal("ItemSet", collection.Type);
        Assert.Equal(["IgnoreNearlyComplete", "RequiresUnlock"], collection.Flags);
        Assert.Equal([1], collection.Prerequisites);
        Assert.Equal([(1, 1), (2, 1)], collection.Tiers.Select(t => (t.Count, t.Points)));
        Assert.Equal([("Skin", 208), ("Item", 70502)], collection.Bits.Select(b => (b.Type, b.Id)));
        Assert.Equal("Dropped by Svanir.", collection.Bits[1].Text);
        Assert.Equal(
            [("Item", (int?)70598, (int?)1, (string?)null), ("Mastery", 3, null, "Tyria")],
            collection.Rewards.Select(r => (r.Type, r.Id, r.Count, r.Region))
        );

        Achievement slayer = await db.Achievements.SingleAsync(a => a.Id == 1);
        Assert.Empty(slayer.Bits);
        Assert.Empty(slayer.Rewards);
        Assert.Null(slayer.Icon);
    }

    [Fact]
    public async Task Updates_achievements_it_has_seen_before()
    {
        StubApi();
        await SyncAsync();

        StubApi(centaurName: "Centaur Slayer II");
        await SyncAsync();

        using Gw2GizmosDbContext db = _fixture.NewContext();
        Assert.Equal(2, await db.Achievements.CountAsync());
        Assert.Equal("Centaur Slayer II", (await db.Achievements.SingleAsync(a => a.Id == 1)).Name);
    }
}
