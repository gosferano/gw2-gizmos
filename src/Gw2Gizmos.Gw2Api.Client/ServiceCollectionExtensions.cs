using Gw2Gizmos.Gw2Api.Client.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Gw2Api.Client;

/// <summary>DI registration for the GW2 API client.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGw2ApiClientFactory"/> over a managed HttpClient (base address, user-agent,
    /// and resilience — retries transient errors + HTTP 429 honoring <c>Retry-After</c>, plus a
    /// per-attempt timeout). Inject <see cref="IGw2ApiClientFactory"/> and call <c>Create(token)</c>.
    /// </summary>
    public static IServiceCollection AddGw2ApiClient(this IServiceCollection services) =>
        services.AddGw2ApiClient(static _ => { });

    /// <summary>
    /// As <see cref="AddGw2ApiClient(IServiceCollection)"/>, exposing the <see cref="IHttpClientBuilder"/>
    /// for advanced customization (custom primary handler, proxy, extra delegating handlers, etc.).
    /// </summary>
    public static IServiceCollection AddGw2ApiClient(
        this IServiceCollection services,
        Action<IHttpClientBuilder> configureHttpClient
    )
    {
        IHttpClientBuilder builder = services
            .AddHttpClient(
                Gw2ApiClientFactory.HttpClientName,
                client =>
                {
                    client.BaseAddress = new Uri(Gw2ApiClient.BaseUrl);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Gw2Gizmos");
                }
            )
            .AddPolicyHandler(Gw2ApiResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(Gw2ApiResiliencePolicies.GetTimeoutPolicy());

        configureHttpClient(builder);

        services.AddSingleton<IGw2ApiClientFactory, Gw2ApiClientFactory>();
        return services;
    }
}
