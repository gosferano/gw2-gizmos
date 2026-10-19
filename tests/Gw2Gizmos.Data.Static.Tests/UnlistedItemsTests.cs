using Gw2Gizmos.Data.Static.Achievements;

namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>The embedded names of achievement items the API doesn't list load, by their in-game name.</summary>
public class UnlistedItemsTests
{
    [Fact]
    public void Names_load_from_the_embedded_data()
    {
        Assert.True(UnlistedItems.NamesById.Count > 1000);
        Assert.Equal("Eir's Advice", UnlistedItems.NamesById[76585]); // Bolt I: The Experimental Sword
        Assert.Equal("Karka Hatchling", UnlistedItems.NamesById[69815]); // the wiki's page says "(trophy 38)"
    }
}
