using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Client.Json;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BaseClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            Converters =
            {
                new StringValueStructConverterFactory(),
                new ItemJsonConverter(),
                new GuildUpgradeJsonConverter()
            },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            TypeInfoResolver = Gw2ApiV2JsonContext.Default
        };
    private static readonly JsonSerializerContext JsonSerializerContext = new Gw2ApiV2JsonContext(
        JsonSerializerOptions
    );

    protected readonly HttpClient HttpClient;
    private readonly string _idsParameterName;

    private const string SchemaVersionHeaderName = "X-Schema-Version";
    protected const string SchemaVersion = "2022-03-23T19:00:00.000Z";

    internal BaseClient(HttpClient httpClient, string idsParameterName = "ids")
    {
        HttpClient = httpClient;
        _idsParameterName = idsParameterName;
    }

    protected abstract string UriPath { get; }

    protected Task<TResponse> GetBlob<TResponse>(CancellationToken cancellationToken)
    {
        return Get<TResponse>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<TResponse> GetById<TResponse, TId>(TId id, CancellationToken cancellationToken)
    {
        return Get<TResponse>($"{UriPath}/{id}", SchemaVersion, cancellationToken);
    }

    protected Task<TId[]> GetIds<TId>(CancellationToken cancellationToken)
    {
        return Get<TId[]>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<TResponse[]> GetByIds<TResponse, TId>(IEnumerable<TId> ids, CancellationToken cancellationToken)
    {
        return Get<TResponse[]>(
            $"{UriPath}?{_idsParameterName}=" + string.Join(",", ids),
            SchemaVersion,
            cancellationToken
        );
    }

    protected Task<TResponse[]> GetPage<TResponse>(int page, CancellationToken cancellationToken)
    {
        return Get<TResponse[]>($"{UriPath}?page={page}", SchemaVersion, cancellationToken);
    }

    protected Task<TResponse[]> GetPage<TResponse>(int page, int pageSize, CancellationToken cancellationToken)
    {
        return Get<TResponse[]>($"{UriPath}?page={page}&page_size={pageSize}", SchemaVersion, cancellationToken);
    }

    protected Task<TResponse[]> GetAll<TResponse>(CancellationToken cancellationToken)
    {
        return Get<TResponse[]>($"{UriPath}?{_idsParameterName}=all", SchemaVersion, cancellationToken);
    }

    protected async Task<TResponse> Get<TResponse>(
        string uri,
        string schemaVersion,
        CancellationToken cancellationToken
    )
    {
        HttpRequestMessage request =
            new(HttpMethod.Get, uri) { Headers = { { SchemaVersionHeaderName, schemaVersion } } };
        using HttpResponseMessage response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>(JsonSerializerContext.Options, cancellationToken))!;
    }
}
