namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpSeasonLeaderboardSettingsTier
{
    public string? Color { get; set; }
    public PvpSeasonLeaderboardSettingsTierType Type { get; set; } // test it
    public string? Name { get; set; }
    public decimal[] Range { get; set; } = Array.Empty<decimal>();
}
