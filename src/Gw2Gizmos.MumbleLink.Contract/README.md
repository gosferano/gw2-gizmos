# Gw2Gizmos.MumbleLink.Contract

Snapshot DTOs and value types for the Guild Wars 2 **MumbleLink** shared-memory block — the live in-game state the
game publishes while running (the `LinkedMem` struct: tick, avatar/camera pose, the identity JSON, and the
game-specific `MumbleContext`).

This library is **data only**: plain immutable DTOs (`MumbleLinkSnapshot` and friends) plus small value-structs
(`Profession`, `Race`, `MountType`, …) modelled like the GW2 API contract — a single backing `Value` with implicit
conversions, so unknown ids (a spec or mount shipped before the named list is updated) flow through as the raw value
instead of throwing. It has no dependencies and knows nothing about how the block is read.

To read the block into these types, use `Gw2Gizmos.MumbleLink.Client`.
