# Gw2Gizmos.Gw2Api.Contract

Request/response contract types (DTOs) for the [Guild Wars 2 API v2](https://wiki.guildwars2.com/wiki/API:2).

These are the plain data models the API returns. You usually don't reference this package directly — it's
pulled in by **[Gw2Gizmos.Gw2Api.Client](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Client)**, a typed
client that calls the API and hands back these types. Reference it on its own only if you deserialize GW2
responses yourself.

## Deserializing on your own

The DTOs need the bundled converters to deserialize correctly — the GW2 API uses polymorphic objects
(items, skins, skill/trait facts), string-keyed enums, and a few numbers sent as strings. Use the
pre-configured `Gw2ApiContractJson.Options` (converters + snake_case naming + source-generated metadata):

```csharp
using System.Text.Json;
using Gw2Gizmos.Gw2Api.Contract;
using Gw2Gizmos.Gw2Api.Contract.V2.Items;

// json fetched however you like (your own HttpClient, a cache, a webhook, ...)
Item[] items = JsonSerializer.Deserialize<Item[]>(json, Gw2ApiContractJson.Options)!;
```

The same `Options` round-trips on serialize, and resolves each polymorphic payload to its concrete
subtype (`Armor`, `Weapon`, `SkillFactBuff`, …).
