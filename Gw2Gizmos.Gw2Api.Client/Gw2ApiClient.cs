using Gw2Gizmos.Gw2Api.Client.V2;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClient
{
    private const string BaseUrl = "https://api.guildwars2.com";
    private const string AuthorizationHeaderName = "Authorization";
    private const string AcceptLanguageHeaderName = "Accept-Language";
    private const string UserAgentHeaderName = "User-Agent";

    public IGw2ApiV2Client V2 { get; }

    public Gw2ApiClient(HttpClient httpClient)
    {
        V2 = new Gw2ApiV2Client(httpClient);
    }

    public Gw2ApiClient()
        : this(null, null) { }

    public Gw2ApiClient(Locale locale)
        : this(null, locale) { }

    public Gw2ApiClient(string? accessToken, Locale? locale = null)
        : this(BuildDefaultHttpClient(accessToken, locale)) { }

    private static HttpClient BuildDefaultHttpClient(string? accessToken, Locale? locale)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl), };

        httpClient.DefaultRequestHeaders.Add(UserAgentHeaderName, "Gw2Gizmos");

        if (accessToken != null)
        {
            httpClient.DefaultRequestHeaders.Add(AuthorizationHeaderName, $"Bearer {accessToken}");
        }

        if (locale != null)
        {
            httpClient.DefaultRequestHeaders.Add(AcceptLanguageHeaderName, locale);
        }

        return httpClient;
    }
}
