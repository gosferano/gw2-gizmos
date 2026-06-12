# Gw2Gizmos.Gw2Api.Contract

Request/response contract types (DTOs) for the [Guild Wars 2 API v2](https://wiki.guildwars2.com/wiki/API:2).

These are the plain data models the API returns. You usually don't reference this package directly — it's
pulled in by **[Gw2Gizmos.Gw2Api.Client](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Client)**, a typed
client that calls the API and hands back these types. Reference it on its own only if you deserialize GW2
responses yourself.
