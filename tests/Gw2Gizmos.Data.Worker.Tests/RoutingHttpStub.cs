using System.Net;
using System.Text;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// A primary HTTP handler that routes by the request's absolute path and returns a canned JSON body (200,
/// application/json). Per-test configurable via <see cref="SetJson"/> so different syncs return different holdings;
/// the same handler instance reused across two SyncAccount calls models "the account state changed between syncs".
/// Unmapped paths return an empty 200 array so an unexpected endpoint deserializes to an empty collection rather
/// than throwing — and any 404 would just no-op the section (the client maps non-200 to a null result).
/// </summary>
internal sealed class RoutingHttpStub : HttpMessageHandler
{
    private readonly Dictionary<string, string> _byPath = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every absolute path requested, in order — for diagnostics in tests.</summary>
    public List<string> RequestedPaths { get; } = new();

    /// <summary>Maps an absolute path (e.g. <c>/v2/account/bank</c>) to the JSON its endpoint should return.</summary>
    public void SetJson(string path, string json) => _byPath[path] = json;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // AbsolutePath keeps %-encoding (e.g. a character name's space stays "%20"); decode so the route key
        // matches the human-readable path the test registered.
        string path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
        RequestedPaths.Add(path);
        string body = _byPath.TryGetValue(path, out string? json) ? json : "[]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }

    /// <summary>The real <see cref="IGw2ApiClientFactory"/> (full DI + resilience pipeline) wired to this stub
    /// handler — exactly the client-test pattern, so the updater exercises everything except the socket.</summary>
    public IGw2ApiClientFactory BuildClientFactory()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddGw2ApiClient(builder => builder.ConfigurePrimaryHttpMessageHandler(() => this))
            .BuildServiceProvider();
        return provider.GetRequiredService<IGw2ApiClientFactory>();
    }
}
