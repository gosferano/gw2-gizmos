using System.Net;
using Gw2Gizmos.Gw2Api.Client.V2;
using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

public class ResilienceTests
{
    private const string OneItemJson =
        """[{"id":1,"type":"Armor","name":"Coat","rarity":"Basic","level":0,"vendor_value":0,"chat_link":"[&A=]"}]""";

    [Fact]
    public async Task Rate_limited_request_is_retried_and_then_succeeds()
    {
        // 429 with Retry-After: 0 → the policy retries immediately (no real delay), then 200 succeeds.
        var handler = new StubHttpMessageHandler()
            .ThenWithHeaders(HttpStatusCode.TooManyRequests, h => h.SetRetryAfterSeconds(0))
            .Then(HttpStatusCode.OK, OneItemJson);

        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item[], Error> result = await client.V2.Items.GetByIds([1]);

        Assert.Equal(2, handler.CallCount); // one rate-limited attempt + one successful retry
        Assert.Null(result.Error);
        Assert.Single(result.Content!);
    }

    [Fact]
    public async Task Non_transient_failure_is_not_retried()
    {
        // 404 is not transient and not 429, so the policy must pass it straight through (a single call).
        var handler = new StubHttpMessageHandler()
            .Then(HttpStatusCode.NotFound, """{"text":"nope"}""")
            .Then(HttpStatusCode.OK, OneItemJson); // would only be reached if it (wrongly) retried

        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item[], Error> result = await client.V2.Items.GetByIds([1]);

        Assert.Equal(1, handler.CallCount);
        Assert.Equal("nope", result.Error!.Text);
    }
}
