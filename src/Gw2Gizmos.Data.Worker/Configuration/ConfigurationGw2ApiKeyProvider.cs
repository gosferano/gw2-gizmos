namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Reads the GW2 API key from configuration — <c>Gw2:ApiKey</c> (e.g. user-secrets / appsettings)
/// or the <c>GW2_API_KEY</c> environment variable. The default for headless hosts.
/// </summary>
public sealed class ConfigurationGw2ApiKeyProvider : IGw2ApiKeyProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationGw2ApiKeyProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? GetApiKey() => _configuration["Gw2:ApiKey"] ?? _configuration["GW2_API_KEY"];
}
