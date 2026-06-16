using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMumbleLink_registers_the_reader_as_a_singleton()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using ServiceProvider provider = new ServiceCollection().AddMumbleLink().BuildServiceProvider();

        var first = provider.GetRequiredService<IMumbleLinkReader>();
        var second = provider.GetRequiredService<IMumbleLinkReader>();

        Assert.Same(first, second);
    }
}
