using System.Globalization;
using System.Text;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Builds the minimal-but-valid canned JSON for one account's V2 endpoints and writes it onto a
/// <see cref="RoutingHttpStub"/>. Only the fields the updater reads are emitted. Mutate the builder and re-apply to
/// the same stub to model the account changing between two syncs.
/// <para>
/// JSON field names/casing match the GW2 wire format (snake_case where the API uses it; the contract's
/// JsonSerializerOptions map them). Race/Gender/Profession deserialize from their capitalized string names.
/// </para>
/// </summary>
internal sealed class AccountStateBuilder
{
    private const string AccountId = "ABCDE-ACCOUNT-GUID";
    private const string AccountName = "Tester.1234";
    private const int World = 1001;

    // Permissions the enabled sections need: account + wallet + inventories + characters. (builds is optional —
    // the build/equipment tab counts just come back 0 without it, which is fine for these tests.)
    public List<string> Permissions { get; } = new() { "account", "wallet", "inventories", "characters", "builds" };

    /// <summary>currency id -> balance.</summary>
    public Dictionary<int, long> Wallet { get; } = new();

    /// <summary>material item id -> count (one storage row each).</summary>
    public Dictionary<int, int> Materials { get; } = new();

    /// <summary>Bank slot grid; null = empty slot.</summary>
    public List<ItemSlot?> Bank { get; } = new();

    /// <summary>Shared-inventory slot grid; null = empty slot.</summary>
    public List<ItemSlot?> SharedInventory { get; } = new();

    /// <summary>character name -> bag slot grid (one bag); null entries = empty slots.</summary>
    public Dictionary<string, List<ItemSlot?>> Characters { get; } = new();

    public string Id => AccountId;

    internal sealed record ItemSlot(int Id, int Count, int? Charges = null);

    public void Apply(RoutingHttpStub stub)
    {
        stub.SetJson("/v2/account", AccountJson());
        stub.SetJson("/v2/tokeninfo", TokenInfoJson());
        stub.SetJson("/v2/account/wallet", WalletJson());
        stub.SetJson("/v2/account/materials", MaterialsJson());
        stub.SetJson("/v2/account/bank", SlotArrayJson(Bank));
        stub.SetJson("/v2/account/inventory", SlotArrayJson(SharedInventory));

        stub.SetJson("/v2/characters", CharacterIdsJson());
        foreach ((string name, List<ItemSlot?> bag) in Characters)
        {
            stub.SetJson($"/v2/characters/{name}", CharacterJson(name, bag));
        }
    }

    private static string AccountJson() =>
        $$"""{"id":"{{AccountId}}","name":"{{AccountName}}","world":{{World}},"guilds":[]}""";

    private string TokenInfoJson()
    {
        string perms = string.Join(",", Permissions.Select(p => $"\"{p}\""));
        return $$"""{"id":"KEYID","name":"Test Key","permissions":[{{perms}}],"type":"APIKey"}""";
    }

    private string WalletJson()
    {
        var entries = Wallet.Select(kv => $$"""{"id":{{kv.Key}},"value":{{kv.Value}}}""");
        return "[" + string.Join(",", entries) + "]";
    }

    private string MaterialsJson()
    {
        // category is required-shaped but the updater only reads id + count.
        var entries = Materials.Select(kv => $$"""{"id":{{kv.Key}},"category":1,"count":{{kv.Value}}}""");
        return "[" + string.Join(",", entries) + "]";
    }

    /// <summary>A slot array (bank / shared inventory): item objects with null for empty slots.</summary>
    private static string SlotArrayJson(IReadOnlyList<ItemSlot?> slots)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < slots.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(slots[i] is { } slot ? ItemJson(slot) : "null");
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static string ItemJson(ItemSlot slot)
    {
        string charges = slot.Charges is { } c ? $",\"charges\":{c}" : "";
        return $$"""{"id":{{slot.Id}},"count":{{slot.Count}}{{charges}}}""";
    }

    private string CharacterIdsJson() =>
        "[" + string.Join(",", Characters.Keys.Select(n => $"\"{n}\"")) + "]";

    private static string CharacterJson(string name, IReadOnlyList<ItemSlot?> bagSlots)
    {
        // One bag holding the slot grid. Minimal core fields the upsert reads; dates as ISO-8601.
        string created = "2020-01-01T00:00:00Z";
        string modified = "2026-01-01T00:00:00Z";
        return $$"""
            {
              "name": "{{name}}",
              "race": "Asura",
              "gender": "Female",
              "profession": "Necromancer",
              "level": 80,
              "guild": null,
              "age": 123456,
              "created": "{{created}}",
              "last_modified": "{{modified}}",
              "deaths": 7,
              "title": null,
              "build_tabs_unlocked": 1,
              "active_build_tab": 1,
              "equipment_tabs_unlocked": 1,
              "active_equipment_tab": 1,
              "bags": [
                { "id": 0, "size": {{bagSlots.Count}}, "inventory": {{SlotArrayJson(bagSlots)}} }
              ]
            }
            """;
    }
}
