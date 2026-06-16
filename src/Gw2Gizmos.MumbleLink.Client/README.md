# Gw2Gizmos.MumbleLink.Client

Reads the Guild Wars 2 **MumbleLink** shared-memory block into the immutable `MumbleLinkSnapshot` DTOs from
`Gw2Gizmos.MumbleLink.Contract`.

```csharp
using IMumbleLinkReader reader = MumbleLinkReader.Create();

if (reader.TryRead(out MumbleLinkSnapshot? snapshot))
{
    // snapshot.UiTick advances while the game is live; snapshot.Identity?.Name is the current character.
}
```

Or via dependency injection (registers a singleton reader that keeps the shared-memory view open across reads):

```csharp
services.AddMumbleLink();
```

`Read()` returns `null` and `TryRead` returns `false` when the game is not running (the mapped file is absent) or
the block has not been populated yet — these are expected, not errors.

**Windows only.** The reader opens the named memory-mapped file the game publishes; its surface is annotated
`[SupportedOSPlatform("windows")]`. The contract package is fully portable. The byte-level parsing
(`Marshalling/`) is implemented as pure functions over `ReadOnlySpan<byte>`/`string`, so it is unit-testable
without the game running.
