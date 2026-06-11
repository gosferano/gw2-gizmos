using Gw2Gizmos.Gw2Api.Client.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Gw2Api.Client;

/// <summary>DI registration for the GW2 API client.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGw2ApiClientFactory"/> over a named HttpClient with built-in resilience:
    /// retries transient errors and HTTP 429 (rate limit) honoring <c>Retry-After</c>, plus a
    /// per-attempt timeout. Inject <see cref="IGw2ApiClientFactory"/> to create clients.
    /// </summary>
    public static IServiceCollection AddGw2ApiClient(this IServiceCollection services)
    {
        services
            .AddHttpClient("Gw2Api")
            .AddPolicyHandler(Gw2ApiResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(Gw2ApiResiliencePolicies.GetTimeoutPolicy());

        services.AddSingleton<IGw2ApiClientFactory, Gw2ApiClientFactory>();
        return services;
    }
}
