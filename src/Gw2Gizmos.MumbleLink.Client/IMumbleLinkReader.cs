using System.Diagnostics.CodeAnalysis;

namespace Gw2Gizmos.MumbleLink.Client;

/// <summary>
/// Reads the live <c>MumbleLink</c> shared-memory block the Guild Wars 2 client publishes. Every member is safe to
/// call when the game is not running: there is no mapped file then, which is an expected state rather than an error
/// — <see cref="Read"/> returns <c>null</c> and <see cref="TryRead"/> returns <c>false</c>. The reader keeps the
/// shared-memory view open across reads, so dispose it when done.
/// </summary>
public interface IMumbleLinkReader : IDisposable
{
    /// <summary>
    /// Reads the current snapshot, or <c>null</c> when the game is not running (no mapped file) or the block has
    /// not been populated yet.
    /// </summary>
    MumbleLinkSnapshot? Read();

    /// <summary>The try-pattern form of <see cref="Read"/>; <c>false</c> when no snapshot is available.</summary>
    bool TryRead([NotNullWhen(true)] out MumbleLinkSnapshot? snapshot);
}
