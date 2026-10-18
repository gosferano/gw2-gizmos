using System.Collections.Generic;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The WvW teams' names. The API gives teams only as ids (matches list them among their worlds, accounts and guilds by
/// id), so the names come from the wiki's table of team ids (API:2/wvw/guilds/:region).
/// </summary>
public static class WvwTeams
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [11001] = "Moogooloo",
        [11002] = "Rall's Rest",
        [11003] = "Domain of Torment",
        [11004] = "Yohlon Haven",
        [11005] = "Tombs of Drascir",
        [11006] = "Hall of Judgment",
        [11007] = "Throne of Balthazar",
        [11008] = "Dwayna's Temple",
        [11009] = "Abaddon's Prison",
        [11010] = "Cathedral of Blood",
        [11011] = "Lutgardis Conservatory",
        [11012] = "Mosswood",
        [12001] = "Skrittsburgh",
        [12002] = "Fortune's Vale",
        [12003] = "Silent Woods",
        [12004] = "Ettin's Back",
        [12005] = "Domain of Anguish",
        [12006] = "Palawadan",
        [12007] = "Bloodstone Gulch",
        [12008] = "Frost Citadel",
        [12009] = "Dragrimmar",
        [12010] = "Grenth's Door",
        [12011] = "Mirror of Lyssa",
        [12012] = "Melandru's Dome",
        [12013] = "Kormir's Library",
        [12014] = "Great House Aviary",
        [12015] = "Bava Nisos",
    };

    /// <summary>
    /// Team ids are the 11xxx (NA) and 12xxx (EU) ones; matches list the teams' old world ids beside them.
    /// </summary>
    public static bool IsTeam(int id) => id is > 11000 and < 13000;

    /// <summary>The team's name, or "Team &lt;id&gt;" for one the table doesn't know yet.</summary>
    public static string NameOf(int teamId) => Names.TryGetValue(teamId, out string? name) ? name : $"Team {teamId}";
}
