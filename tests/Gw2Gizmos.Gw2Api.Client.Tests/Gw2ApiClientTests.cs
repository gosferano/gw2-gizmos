using System.Net;
using Gw2Gizmos.Gw2Api.Client.V2;
using Gw2Gizmos.Gw2Api.Contract.V2;
using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

public class Gw2ApiClientTests
{
    private const string TwoItemsJson = """
        [
          {"id":1,"type":"Armor","name":"Test Coat","rarity":"Exotic","level":80,"vendor_value":100,"chat_link":"[&AgEAAAA=]"},
          {"id":2,"type":"Weapon","name":"Test Sword","rarity":"Rare","level":80,"vendor_value":50,"chat_link":"[&AgEBAAA=]"}
        ]
        """;

    [Fact]
    public async Task GetByIds_deserializes_polymorphic_items_into_their_concrete_types()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoItemsJson);
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item[], Error> result = await client.V2.Items.GetByIds([1, 2]);

        Assert.Null(result.Error);
        Item[] items = result.Content!;
        Assert.Equal(2, items.Length);
        Assert.IsType<Armor>(items[0]);
        Assert.IsType<Weapon>(items[1]);
        Assert.Equal("Test Coat", items[0].Name);
        Assert.Equal(ItemRarity.Rare, items[1].Rarity);
    }

    [Fact]
    public async Task GetByIds_targets_the_bulk_url_with_the_schema_and_user_agent_headers()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoItemsJson);
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        await client.V2.Items.GetByIds([1, 2]);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal("/v2/items?ids=1,2", request.RequestUri!.PathAndQuery);
        Assert.Equal("2022-03-23T19:00:00.000Z", Assert.Single(request.Headers.GetValues("X-Schema-Version")));
        Assert.Contains("Gw2Gizmos", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Create_with_token_sends_a_bearer_authorization_header()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoItemsJson);
        Gw2ApiClient client = TestClientBuilder.Build(handler, accessToken: "secret-key");

        await client.V2.Items.GetByIds([1]);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer secret-key", Assert.Single(request.Headers.GetValues("Authorization")));
    }

    [Fact]
    public async Task Create_without_token_sends_no_authorization_header()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoItemsJson);
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        await client.V2.Items.GetByIds([1]);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.False(request.Headers.Contains("Authorization"));
    }

    [Fact]
    public void Static_Create_yields_a_usable_client_without_DI()
    {
        // Reuses the process-wide default factory; no network until a call is made.
        Gw2ApiClient client = Gw2ApiClient.Create();

        Assert.NotNull(client.V2);
        Assert.NotNull(client.V2.Items);
    }
}
