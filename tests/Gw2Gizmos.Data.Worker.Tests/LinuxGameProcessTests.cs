namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>Recognising the game among Linux processes by the executable in their command line.</summary>
public sealed class LinuxGameProcessTests
{
    [Theory]
    // The game under Proton, by its Windows path.
    [InlineData("S:\\steamapps\\common\\Guild Wars 2\\Gw2-64.exe\0", true)]
    // Steam's launch wrapper, by the host path among its arguments.
    [InlineData("/home/u/.local/share/Steam/ubuntu12_32/reaper\0SteamLaunch\0AppId=1284210\0--\0/home/u/.local/share/Steam/steamapps/common/Guild Wars 2/Gw2-64.exe\0", true)]
    // Lutris or plain Wine: case differs.
    [InlineData("wine\0C:\\Program Files\\Guild Wars 2\\gw2-64.EXE\0-autologin\0", true)]
    // Only mentions the name inside a longer argument (e.g. a shell command), or another file with it as a prefix.
    [InlineData("/usr/bin/zsh\0-c\0pgrep -f Gw2-64.exe\0", false)]
    [InlineData("/usr/bin/tail\0/home/u/Gw2-64.exe.log\0", false)]
    [InlineData("", false)]
    public void Recognises_the_game_by_its_executable(string commandLine, bool expected) =>
        Assert.Equal(expected, LinuxGameProcess.IsGameCommandLine(commandLine));
}
