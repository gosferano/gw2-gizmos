namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Placing MumbleLink's avatar position on the continent map, where the game leaves its own out (WvW).</summary>
public sealed class MapGeometryTests
{
    // Eternal Battlegrounds: ±36864 inches in its own coordinates, x 8958–12030 and y 12798–15870 on the Mists.
    private static readonly MapFrames EternalBattlegrounds = new(
        -36864,
        -36864,
        36864,
        36864,
        8958,
        12798,
        12030,
        15870
    );

    [Fact]
    public void The_maps_middle_lands_on_the_middle_of_its_continent_rectangle()
    {
        Assert.Equal((10494.0, 14334.0), EternalBattlegrounds.ToContinent(0, 0));
    }

    [Fact]
    public void Metres_become_inches_and_north_points_up_the_continent_map()
    {
        // 744.6 m north and 31.3 m east of the middle, as read in game near Stonemist's north.
        (double x, double y) = EternalBattlegrounds.ToContinent(31.2926, 744.6429);

        Assert.Equal(10545.5, x, 0.5);
        Assert.Equal(13112.5, y, 0.5);
    }
}
