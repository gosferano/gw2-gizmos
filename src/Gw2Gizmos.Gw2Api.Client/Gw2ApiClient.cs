using Gw2Gizmos.Gw2Api.Client.V2;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClient
{
    internal const string BaseUrl = "https://api.guildwars2.com";
    internal const string AuthorizationHeaderName = "Authorization";
    internal const string AcceptLanguageHeaderName = "Accept-Language";

    // Built once on first use so the simple Create(...) path reuses one pooled, resilient HttpClient
    // infrastructure instead of standing one up per call.
    private static readonly Lazy<IGw2ApiClientFactory> DefaultFactory = new(Gw2ApiClientFactory.CreateDefault);

    public IGw2ApiV2Client V2 { get; }

    // Constructed only by Gw2ApiClientFactory (registered via AddGw2ApiClient), so the HttpClient — and
    // its base address, auth, and headers — stay the library's concern, never the caller's.
    internal Gw2ApiClient(HttpClient httpClient)
    {
        V2 = new Gw2ApiV2Client(httpClient);
    }

    /// <summary>
    /// Creates a client with no DI setup, reusing a process-wide default factory (a managed, resilient
    /// HttpClient). Convenient for scripts and CLIs. Apps using dependency injection should prefer
    /// <c>AddGw2ApiClient</c> + <see cref="IGw2ApiClientFactory"/> so the HttpClient follows their host.
    /// </summary>
    public static Gw2ApiClient Create(string? accessToken = null, Locale? locale = null) =>
        DefaultFactory.Value.Create(accessToken, locale);
}
