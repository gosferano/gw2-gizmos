using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Client.Clients.Account;
using Gw2Gizmos.Gw2Api.Client.Clients.Items;
using Gw2Gizmos.Gw2Api.Client.Json;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClient : IGw2ApiClient
{
    private const string BaseUrl = "https://api.guildwars2.com";
    private const string AuthorizationHeaderName = "Authorization";
    private const string AcceptLanguageHeaderName = "Accept-Language";
    private const string SchemaVersionHeaderName = "X-Schema-Version";

    public IAccountClient Account { get; }
    public IItemsClient Items { get; }

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            Converters = { new StringValueStructConverterFactory(), new ItemJsonConverter() },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            TypeInfoResolver = Gw2ApiV2JsonContext.Default
        };
    private readonly JsonSerializerContext _jsonSerializerContext;

    public Gw2ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSerializerContext = new Gw2ApiV2JsonContext(_jsonSerializerOptions);

        // Initialize clients
        Account = new AccountClient(this);
        Items = new ItemsClient(this);
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

    public async Task<TResponse> Get<TResponse>(string uri, string schemaVersion, CancellationToken cancellationToken)
    {
        HttpRequestMessage request =
            new(HttpMethod.Get, uri) { Headers = { { SchemaVersionHeaderName, schemaVersion } } };
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (
            await response.Content.ReadFromJsonAsync<TResponse>(_jsonSerializerContext.Options, cancellationToken)
        )!;
    }
}

internal interface IGw2ApiClient
{
    Task<T> Get<T>(string uri, string schemaVersion, CancellationToken cancellationToken);
}
