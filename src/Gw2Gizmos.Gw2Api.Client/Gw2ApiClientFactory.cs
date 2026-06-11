using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClientFactory : IGw2ApiClientFactory
{
    /// <summary>Name of the HttpClient configured by AddGw2ApiClient. Single source for both sides.</summary>
    internal const string HttpClientName = "Gw2Gizmos.Gw2Api";

    private readonly IHttpClientFactory _httpClientFactory;

    public Gw2ApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Creates a self-contained factory (default managed HttpClient + resilience) for apps that don't
    /// use dependency injection. Build it <b>once</b> and reuse it for the app's lifetime — each call
    /// stands up its own HttpClient infrastructure. DI apps should prefer <c>AddGw2ApiClient</c> instead.
    /// </summary>
    public static IGw2ApiClientFactory CreateDefault()
    {
        return new ServiceCollection()
            .AddGw2ApiClient()
            .BuildServiceProvider()
            .GetRequiredService<IGw2ApiClientFactory>();
    }

    public Gw2ApiClient Create(Locale locale)
    {
        return Create(null, locale);
    }

    public Gw2ApiClient Create(string? accessToken, Locale? locale = null)
    {
        // The named client already has the base address, user-agent, and resilience configured
        // (see AddGw2ApiClient); here we only add the per-call auth and locale.
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);

        if (accessToken != null)
        {
            httpClient.DefaultRequestHeaders.Add(Gw2ApiClient.AuthorizationHeaderName, $"Bearer {accessToken}");
        }

        if (locale != null)
        {
            httpClient.DefaultRequestHeaders.Add(Gw2ApiClient.AcceptLanguageHeaderName, locale);
        }

        return new Gw2ApiClient(httpClient);
    }
}
