using System.Net;
using Gw2Gizmos.Gw2Api.Client.V2;
using Gw2Gizmos.Gw2Api.Contract.V2;
using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

public class ErrorHandlingTests
{
    // 404 is non-transient, so these never hit the retry policy (fast + deterministic).
    [Fact]
    public async Task Error_response_with_a_text_body_is_surfaced_as_Error_not_thrown()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.NotFound, """{"text":"no such id"}""");
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item, Error> result = await client.V2.Items.GetById(999999);

        Assert.Null(result.Content);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("no such id", result.Error!.Text);
    }

    [Fact]
    public async Task Empty_error_body_synthesizes_a_status_based_message()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.NotFound, "");
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item, Error> result = await client.V2.Items.GetById(1);

        Assert.Equal("HTTP 404 Not Found", result.Error!.Text);
    }

    [Fact]
    public async Task Non_json_error_body_does_not_throw_and_falls_back_to_status_message()
    {
        // e.g. an HTML 5xx page from an intermediary proxy.
        var handler = StubHttpMessageHandler.Always(
            HttpStatusCode.NotFound,
            "<html><body>Not Found</body></html>",
            mediaType: "text/html"
        );
        Gw2ApiClient client = TestClientBuilder.Build(handler);

        Result<Item, Error> result = await client.V2.Items.GetById(1);

        Assert.NotNull(result.Error);
        Assert.Equal("HTTP 404 Not Found", result.Error!.Text);
    }
}
