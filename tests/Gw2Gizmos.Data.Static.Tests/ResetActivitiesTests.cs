using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Data.Static.Resets;

namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>
/// The names and links kept for the API's once-per-reset activity ids: each points at an event the Events page
/// schedules, and ids without a kept name read from the id.
/// </summary>
public sealed class ResetActivitiesTests
{
    [Fact]
    public void Every_linked_world_boss_and_map_chest_is_a_scheduled_event()
    {
        HashSet<string> scheduled = EventSchedule.All.Select(e => e.Id).ToHashSet();

        Assert.All(ResetActivities.WorldBossEvents.Values, id => Assert.Contains(id, scheduled));
        Assert.All(ResetActivities.MapChestEvents.Values, id => Assert.Contains(id, scheduled));
        Assert.All(ResetActivities.MapChestEvents.Keys, id => Assert.Contains(id, ResetActivities.MapChestMaps.Keys));
        // A meta not on a timer has no scheduled event.
        Assert.All(ResetActivities.UntimedMapChests, id => Assert.Contains(id, ResetActivities.MapChestMaps.Keys));
        Assert.All(
            ResetActivities.UntimedMapChests, id => Assert.DoesNotContain(id, ResetActivities.MapChestEvents.Keys));
    }

    [Theory]
    [InlineData("spirit_vale", "Spirit Vale")]
    [InlineData("the_key_of_ahdashim", "The Key of Ahdashim")]
    [InlineData("qadim_the_peerless", "Qadim the Peerless")]
    [InlineData("voice_in_the_void", "Dhuum")]
    public void A_raid_name_is_its_id_spelled_out_unless_kept(string id, string name)
    {
        Assert.Equal(name, ResetActivities.RaidName(id));
    }
}
