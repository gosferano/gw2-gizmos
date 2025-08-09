namespace Gw2Gizmos.Gw2Api.Client;

public interface IGw2ApiClientFactory
{
    Gw2ApiClient Create(Locale locale);
    Gw2ApiClient Create(string? accessToken, Locale? locale = null);
}
