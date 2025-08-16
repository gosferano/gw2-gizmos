namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClientFactory : IGw2ApiClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Gw2ApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Gw2ApiClient Create(Locale locale)
    {
        return Create(null, locale);
    }

    public Gw2ApiClient Create(string? accessToken, Locale? locale = null)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("Gw2Api");

        httpClient.BaseAddress = new Uri(Gw2ApiClient.BaseUrl);

        httpClient.DefaultRequestHeaders.Add(Gw2ApiClient.UserAgentHeaderName, "Gw2Gizmos");

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
