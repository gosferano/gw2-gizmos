using System.Diagnostics;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>Runs a small external tool (notify-send, secret-tool) and captures its output.</summary>
internal static class CommandRunner
{
    /// <summary>The exit code and trimmed stdout, or null when the tool isn't installed or couldn't start.</summary>
    public static async Task<(int ExitCode, string Output)?> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? standardInput = null,
        TimeSpan? timeout = null
    )
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Not installed / not on PATH.
            return null;
        }

        if (process is null)
        {
            return null;
        }

        using (process)
        {
            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput);
                process.StandardInput.Close();
            }

            using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            try
            {
                Task<string> output = process.StandardOutput.ReadToEndAsync(cancellation.Token);
                await process.WaitForExitAsync(cancellation.Token);
                return (process.ExitCode, (await output).Trim());
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Exited in the meantime.
                }

                return null;
            }
        }
    }

    /// <summary>Synchronous wrapper for callers on a lock-protected, synchronous path (the key store).</summary>
    public static (int ExitCode, string Output)? Run(
        string fileName,
        IEnumerable<string> arguments,
        string? standardInput = null
    ) => Task.Run(() => RunAsync(fileName, arguments, standardInput)).GetAwaiter().GetResult();
}
