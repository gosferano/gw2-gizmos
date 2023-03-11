using System.Text.Json;
using Gw2Sharp.Json;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClient
{
    private HttpClient _httpClient;
    
    public Gw2ApiClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.guildwars2.com"),
        };
    }

    public async Task<T> Get<T>(string uri)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content.ReadAsStreamAsync();
        return (await JsonSerializer.DeserializeAsync<T>(contentStream, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.SnakeCase
        }))!;
    }
}