using System.Net;
using System.Net.Http.Headers;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

/// <summary>
/// A test double for the client's primary handler: serves scripted responses and records every
/// request, so tests can assert on URLs/headers and on how many attempts the resilience policy made.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public int CallCount => Requests.Count;

    /// <summary>Always responds the same way.</summary>
    public static StubHttpMessageHandler Always(HttpStatusCode status, string body, string mediaType = "application/json")
    {
        var handler = new StubHttpMessageHandler();
        handler._responses.Enqueue(_ => Build(status, body, mediaType));
        handler._repeatLast = true;
        return handler;
    }

    private bool _repeatLast;
    private Func<HttpRequestMessage, HttpResponseMessage>? _last;

    /// <summary>Responds with each queued response in turn (for retry/sequence tests).</summary>
    public StubHttpMessageHandler Then(HttpStatusCode status, string body = "", string mediaType = "application/json")
    {
        _responses.Enqueue(_ => Build(status, body, mediaType));
        return this;
    }

    public StubHttpMessageHandler ThenWithHeaders(
        HttpStatusCode status,
        Action<HttpResponseHeaders> configureHeaders,
        string body = ""
    )
    {
        _responses.Enqueue(_ =>
        {
            HttpResponseMessage response = Build(status, body, "application/json");
            configureHeaders(response.Headers);
            return response;
        });
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count > 0)
        {
            _last = _responses.Dequeue();
        }

        if (_last is null)
        {
            throw new InvalidOperationException("StubHttpMessageHandler ran out of scripted responses.");
        }

        // Re-queue so a single Always(...) response keeps serving subsequent calls.
        Func<HttpRequestMessage, HttpResponseMessage> responder = _last;
        if (_repeatLast)
        {
            _responses.Enqueue(responder);
        }

        return Task.FromResult(responder(request));
    }

    private static HttpResponseMessage Build(HttpStatusCode status, string body, string mediaType) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType) };
}

internal static class HttpResponseHeadersExtensions
{
    // Convenience for the Retry-After header used by the 429 resilience test.
    public static void SetRetryAfterSeconds(this HttpResponseHeaders headers, int seconds) =>
        headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));
}
