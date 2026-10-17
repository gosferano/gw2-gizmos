namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Whether Guild Wars 2 is running on Linux, where it runs under Wine/Proton: the process id MumbleLink reports is
/// Wine's, not the host's, so it can't be looked up — but the game is still an ordinary host process with
/// <c>Gw2-64.exe</c> among its arguments (as are Steam's launch wrappers, which exit with it). Found by scanning
/// <c>/proc</c>, then cheaply re-checked by remembering the process it found.
/// </summary>
internal static class LinuxGameProcess
{
    private const string ExecutableName = "Gw2-64.exe";

    private static int? _foundPid;

    public static bool IsRunning()
    {
        if (_foundPid is { } pid && IsGame(pid))
        {
            return true;
        }

        _foundPid = null;
        foreach (string directory in Directory.EnumerateDirectories("/proc"))
        {
            if (int.TryParse(Path.GetFileName(directory), out int candidate) && IsGame(candidate))
            {
                _foundPid = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a <c>/proc/&lt;pid&gt;/cmdline</c> (NUL-separated arguments) runs the game: one argument is the game's
    /// executable, by its file name after the last <c>/</c> or <c>\</c> — not merely mentioning it (e.g. a shell
    /// command containing the name).
    /// </summary>
    internal static bool IsGameCommandLine(string commandLine) =>
        commandLine.Split('\0').Any(argument =>
            argument[(argument.LastIndexOfAny(['/', '\\']) + 1)..].Equals(ExecutableName, StringComparison.OrdinalIgnoreCase));

    private static bool IsGame(int pid)
    {
        try
        {
            return IsGameCommandLine(File.ReadAllText($"/proc/{pid}/cmdline"));
        }
        catch (Exception)
        {
            // Gone meanwhile, or not readable.
            return false;
        }
    }
}
