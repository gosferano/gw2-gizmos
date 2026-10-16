using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Desktop.Core.Platform;

namespace Gw2Gizmos.Desktop.Platform;

/// <summary><see cref="ISecretProtector"/> over Windows DPAPI (per-user; see <see cref="ProtectedDataHelper"/>).</summary>
public sealed class DpapiSecretProtector : ISecretProtector
{
    public string Protect(string plaintext) => ProtectedDataHelper.Protect(plaintext);

    public string? Unprotect(string protectedText) => ProtectedDataHelper.Unprotect(protectedText);
}
