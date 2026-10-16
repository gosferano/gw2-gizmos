using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// <see cref="ISecretProtector"/> backed by the desktop keyring (the freedesktop Secret Service — GNOME Keyring,
/// KWallet, KeePassXC…) via <c>secret-tool</c>. The secret itself lives in the keyring, unlocked with the user's
/// session; the key file only holds a marker pointing at it. Without a keyring (no secret-tool, or no Secret Service
/// running) it falls back to storing the text as-is, relying on the owner-only data directory, and logs that.
/// </summary>
internal sealed class SecretServiceProtector : ISecretProtector
{
    private const string Command = "secret-tool";
    private const string KeyringMarker = "secret-service:v1";
    private const string PlainPrefix = "plain:v1:";

    private readonly string[] _attributes;
    private readonly string _label;
    private readonly ILogger<SecretServiceProtector> _logger;

    /// <param name="scope">Keeps dev and release builds' secrets apart (e.g. the app-data folder name).</param>
    public SecretServiceProtector(string scope, ILogger<SecretServiceProtector> logger)
    {
        _attributes = ["application", "gw2gizmos", "scope", scope, "secret", "api-keys"];
        _label = $"{scope} API keys";
        _logger = logger;
    }

    public string Protect(string plaintext)
    {
        (int ExitCode, string Output)? stored =
            CommandRunner.Run(Command, ["store", $"--label={_label}", .. _attributes], plaintext);
        if (stored is { ExitCode: 0 })
        {
            return KeyringMarker;
        }

        _logger.LogWarning(
            "No desktop keyring available (secret-tool missing or no Secret Service running); storing API keys "
                + "unencrypted in the owner-only data directory instead.");
        return PlainPrefix + plaintext;
    }

    public string? Unprotect(string protectedText)
    {
        if (protectedText.StartsWith(PlainPrefix, StringComparison.Ordinal))
        {
            return protectedText[PlainPrefix.Length..];
        }

        if (protectedText != KeyringMarker)
        {
            return null;
        }

        (int ExitCode, string Output)? lookedUp = CommandRunner.Run(Command, ["lookup", .. _attributes]);
        return lookedUp is { ExitCode: 0, Output.Length: > 0 } ? lookedUp.Value.Output : null;
    }
}
