namespace Gw2Gizmos.Desktop.Core.Platform;

/// <summary>Encrypts small secrets (the stored API keys) at rest, readable back only by the same OS user.</summary>
public interface ISecretProtector
{
    /// <summary>The protected form of <paramref name="plaintext"/>, as text safe to write to a file.</summary>
    string Protect(string plaintext);

    /// <summary>The original text, or null when the blob is corrupt or was protected for a different user/machine.</summary>
    string? Unprotect(string protectedText);
}
