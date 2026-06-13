using System.Security.Cryptography;
using System.Text;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Encrypts/decrypts small secrets with Windows DPAPI (per-user). The ciphertext can only be read
/// back by the same Windows user on the same machine; copying it elsewhere makes it useless.
/// Windows-only at runtime.
/// </summary>
public static class ProtectedDataHelper
{
    // Extra entropy tied to this app; not a secret, just domain separation from other DPAPI blobs.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Gw2Gizmos");

    public static string Protect(string plaintext)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI (ProtectedData) is only available on Windows.");
        }

        byte[] cipher = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext),
            Entropy,
            DataProtectionScope.CurrentUser
        );
        return Convert.ToBase64String(cipher);
    }

    public static string? Unprotect(string base64Cipher)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI (ProtectedData) is only available on Windows.");
        }

        try
        {
            byte[] plain = ProtectedData.Unprotect(
                Convert.FromBase64String(base64Cipher),
                Entropy,
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception)
        {
            // Corrupt blob, or encrypted by a different user/machine — treat as "no key".
            return null;
        }
    }
}
