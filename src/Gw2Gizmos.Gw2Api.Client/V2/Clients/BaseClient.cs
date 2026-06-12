using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Client.Json;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BaseClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters =
        {
            new StringValueStructConverterFactory(),
            new NullableInt32Converter(),
            new ItemJsonConverter(),
            new GuildUpgradeJsonConverter(),
            new ProfessionTrainingTrackStepJsonConverter(),
            new SkillFactJsonConverter(),
            new SkinJsonConverter(),
            new TraitFactJsonConverter(),
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        TypeInfoResolver = Gw2ApiV2JsonContext.Default,
    };
    private static readonly JsonSerializerContext JsonSerializerContext = new Gw2ApiV2JsonContext(
        JsonSerializerOptions
    );

    /// <summary>The exact serializer options used for all API (de)serialization. Internal for tests.</summary>
    internal static JsonSerializerOptions SerializerOptions => JsonSerializerContext.Options;

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

    protected Task<Result<TResponse, Error>> GetBlob<TResponse>(CancellationToken cancellationToken)
    {
        return Get<TResponse>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<Result<TResponse, Error>> GetById<TResponse, TId>(TId id, CancellationToken cancellationToken)
    {
        return Get<TResponse>($"{UriPath}/{id}", SchemaVersion, cancellationToken);
    }

    protected Task<Result<TId[], Error>> GetIds<TId>(CancellationToken cancellationToken)
    {
        return Get<TId[]>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<Result<TResponse[], Error>> GetByIds<TResponse, TId>(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken
    )
    {
        return Get<TResponse[]>(
            $"{UriPath}?{_idsParameterName}=" + string.Join(",", ids),
            SchemaVersion,
            cancellationToken
        );
    }

    protected Task<Result<TResponse[], Error>> GetPage<TResponse>(int page, CancellationToken cancellationToken)
    {
        return Get<TResponse[]>($"{UriPath}?page={page}", SchemaVersion, cancellationToken);
    }

    protected Task<Result<TResponse[], Error>> GetPage<TResponse>(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        return Get<TResponse[]>($"{UriPath}?page={page}&page_size={pageSize}", SchemaVersion, cancellationToken);
    }

    protected Task<Result<TResponse[], Error>> GetAll<TResponse>(CancellationToken cancellationToken)
    {
        return Get<TResponse[]>($"{UriPath}?{_idsParameterName}=all", SchemaVersion, cancellationToken);
    }

    protected async Task<Result<TResponse, Error>> Get<TResponse>(
        string uri,
        string schemaVersion,
        CancellationToken cancellationToken
    )
    {
        HttpRequestMessage request = new(HttpMethod.Get, uri)
        {
            Headers = { { SchemaVersionHeaderName, schemaVersion } },
        };
        using HttpResponseMessage response = await HttpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            Error error = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            return new Result<TResponse, Error>(error, response.StatusCode)
            {
                ResponseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
            };
        }

        var result = await response
            .Content.ReadFromJsonAsync<TResponse>(JsonSerializerContext.Options, cancellationToken)
            .ConfigureAwait(false);
        return new Result<TResponse, Error>(result!, response.StatusCode)
        {
            ResponseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
            ResultTotal =
                response.Headers.TryGetValues("X-Result-Total", out var values)
                && int.TryParse(values.FirstOrDefault(), out int totalPages)
                    ? totalPages
                    : null,
            ResultCount =
                response.Headers.TryGetValues("X-Result-Count", out var countValues)
                && int.TryParse(countValues.FirstOrDefault(), out int count)
                    ? count
                    : null,
        };
    }

    /// <summary>
    /// Reads the error body without throwing. GW2 returns a JSON <c>{"text": ...}</c> on failure, but
    /// a 5xx or an intermediary proxy can return HTML, an empty body, or nothing — in which case a
    /// status-based message is synthesized so the caller always gets a populated <see cref="Error"/>.
    /// </summary>
    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            Error? error = await response
                .Content.ReadFromJsonAsync<Error>(JsonSerializerContext.Options, cancellationToken)
                .ConfigureAwait(false);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Text))
            {
                return error;
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or HttpRequestException or IOException)
        {
            // Unparseable or unreadable error body (malformed JSON, an HTML 5xx page, a mid-stream
            // network drop) — fall through to the status-based message. Cancellation/timeout
            // (OperationCanceledException) is intentionally NOT caught and propagates to the caller.
        }

        return new Error { Text = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Trim() };
    }
}
