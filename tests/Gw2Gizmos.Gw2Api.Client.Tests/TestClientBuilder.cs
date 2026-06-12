using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

/// <summary>Builds a real <see cref="Gw2ApiClient"/> (full DI + resilience pipeline) whose primary
/// handler is a <see cref="StubHttpMessageHandler"/>, so tests exercise everything except the socket.</summary>
internal static class TestClientBuilder
{
    public static Gw2ApiClient Build(HttpMessageHandler handler, string? accessToken = null)
    {
        ServiceProvider provider = new ServiceCollection()
            .AddGw2ApiClient(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler))
            .BuildServiceProvider();

        return provider.GetRequiredService<IGw2ApiClientFactory>().Create(accessToken);
    }
}
