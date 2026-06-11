using System.Net;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Gw2Gizmos.Gw2Api.Client.Http;

/// <summary>Default Polly resilience policies for the GW2 API HTTP client.</summary>
public static class Gw2ApiResiliencePolicies
{
    /// <summary>
    /// Retries transient failures (5xx, 408, network errors), per-attempt timeouts, and rate-limit
    /// responses (HTTP 429). The wait honors the server's <c>Retry-After</c> header when present,
    /// otherwise it backs off exponentially.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx, 408, HttpRequestException
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests) // 429
            .Or<TimeoutRejectedException>() // a per-attempt timeout (see GetTimeoutPolicy)
            .WaitAndRetryAsync(
                retryCount,
                (attempt, outcome, _) =>
                {
                    System.Net.Http.Headers.RetryConditionHeaderValue? retryAfter =
                        outcome.Result?.Headers.RetryAfter;
                    if (retryAfter?.Delta is TimeSpan delta)
                    {
                        return delta;
                    }

                    if (retryAfter?.Date is DateTimeOffset date && date > DateTimeOffset.UtcNow)
                    {
                        return date - DateTimeOffset.UtcNow;
                    }

                    return TimeSpan.FromSeconds(Math.Pow(2, attempt));
                },
                (_, _, _, _) => Task.CompletedTask
            );
    }

    /// <summary>A per-attempt timeout (default 30s) so a stuck request fails fast and is retried.</summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int seconds = 30)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(seconds));
    }
}
